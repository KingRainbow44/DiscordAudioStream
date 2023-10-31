﻿using System.Runtime.InteropServices;

using NAudio.CoreAudioApi;
using NAudio.Wave;

using Windows.Win32.Foundation;

namespace DiscordAudioStream.AudioCapture;

internal class AudioPlayback : IDisposable
{
    public event Action<float, float>? AudioLevelChanged;

    private readonly IWaveIn audioSource;
    private readonly DirectSoundOut output;
    private readonly BufferedWaveProvider outputProvider;
    private readonly CancellationTokenSource audioMeterCancel;

    private static List<MMDevice>? audioDevices;

    public AudioPlayback(int deviceIndex)
    {
        MMDevice device = GetDeviceByIndex(deviceIndex);
        if (device.DataFlow == DataFlow.Render)
        {
            // Input from programs outputting to selected device
            audioSource = new WasapiLoopbackCapture(device);
        }
        else
        {
            // Input from microphone
            audioSource = new WasapiCapture(device);
        }
        audioSource.DataAvailable += AudioSource_DataAvailable;

        Logger.Log("Started audio device: " + device);
        StoreAudioDeviceID(device.ID);

        // Output (to default audio device)
        output = new DirectSoundOut();
        outputProvider = new BufferedWaveProvider(audioSource.WaveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromSeconds(1)
        };
        output.Init(outputProvider);

        // Start a periodic timer to update the audio meter, discard the result
        audioMeterCancel = new();
        _ = UpdateAudioMeter(device, audioMeterCancel.Token);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            audioSource.Dispose();
            output.Dispose();
            audioMeterCancel.Dispose();
        }
    }

    public static string[] RefreshDevices()
    {
        using MMDeviceEnumerator enumerator = new();
        DataFlow flow = Properties.Settings.Default.ShowAudioInputs ? DataFlow.All : DataFlow.Render;
        audioDevices = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active).ToList();

        return audioDevices
            .Select(device => (device.DataFlow == DataFlow.Capture ? "[IN] " : "") + device.FriendlyName)
            .ToArray();
    }

    public static int GetDefaultDeviceIndex()
    {
        if (audioDevices == null)
        {
            throw new InvalidOperationException("RefreshDevices() must be called before calling GetDefaultDeviceIndex");
        }
        using MMDeviceEnumerator enumerator = new();
        if (!enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
        {
            return -1;
        }

        string defaultDeviceId = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
        return audioDevices.FindIndex(device => device.ID == defaultDeviceId);
    }

    public static int GetLastDeviceIndex()
    {
        if (audioDevices == null)
        {
            throw new InvalidOperationException("RefreshDevices() must be called before calling GetLastDeviceIndex");
        }
        string lastDeviceId = Properties.Settings.Default.AudioDeviceID;
        return audioDevices.FindIndex(device => device.ID == lastDeviceId);
    }

    public void Start()
    {
        output.PlaybackStopped += Output_StoppedHandler;
        try
        {
            audioSource.StartRecording();
        }
        catch (COMException e)
        {
            Logger.Log("COMException while starting audio device:");
            Logger.Log(e);
            if (e.ErrorCode == HRESULT.AUDCLNT_E_DEVICE_IN_USE)
            {
                throw new InvalidOperationException("The selected audio device is already in use by another application. Please select a different device.");
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            output.PlaybackStopped -= Output_StoppedHandler;
            throw;
        }
        output.Play();
    }

    public void Stop()
    {
        audioMeterCancel.Cancel();
        audioSource.StopRecording();
        // Remove the handler before stopping manually
        output.PlaybackStopped -= Output_StoppedHandler;
        output.Stop();
    }

    private void AudioSource_DataAvailable(object? sender, WaveInEventArgs e)
    {
        // New audio data available, append to output audio buffer
        outputProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    private void Output_StoppedHandler(object? sender, StoppedEventArgs e)
    {
        // In some cases, streaming to Discord will cause DirectSoundOut to throw an
        // exception and stop. If that happens, just resume playback
        output.Play();
    }

    private async Task UpdateAudioMeter(MMDevice device, CancellationToken token)
    {
        TimeSpan updatePeriod = TimeSpan.FromMilliseconds(10);
        while (!token.IsCancellationRequested)
        {
            bool stereo = device.AudioMeterInformation.PeakValues.Count >= 2;
            float left = stereo
                ? device.AudioMeterInformation.PeakValues[0]
                : device.AudioMeterInformation.MasterPeakValue;
            float right = stereo
                ? device.AudioMeterInformation.PeakValues[1]
                : device.AudioMeterInformation.MasterPeakValue;
            AudioLevelChanged?.Invoke(left, right);
            await Task.Delay(updatePeriod, token).ConfigureAwait(true);
        }
    }

    private static MMDevice GetDeviceByIndex(int index)
    {
        if (audioDevices == null)
        {
            throw new InvalidOperationException("RefreshDevices() must be called before GetDeviceByIndex");
        }
        if (index < 0 || index > audioDevices.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return audioDevices[index];
    }

    private static void StoreAudioDeviceID(string deviceId)
    {
        Logger.Log("Saving audio device ID: " + deviceId);
        Properties.Settings.Default.AudioDeviceID = deviceId;
        Properties.Settings.Default.Save();
    }
}
