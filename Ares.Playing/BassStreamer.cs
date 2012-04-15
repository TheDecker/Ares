﻿/*
 Copyright (c) 2012 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;

namespace Ares.Playing
{
    class BassStreamer : IStreamer
    {
        public void BeginStreaming(StreamingParameters parameters)
        {
            if (IsStreaming)
                EndStreaming();

            m_MixerChannel = Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamCreate(44100, 2, Un4seen.Bass.BASSFlag.BASS_MIXER_RESUME);
            if (m_MixerChannel == 0)
            {
                HandleBassError();
                return;
            }
            Un4seen.Bass.Misc.IBaseEncoder encoder = null;
            switch (parameters.Encoding)
            {
                case StreamEncoding.Wav:
                    encoder = new Un4seen.Bass.Misc.EncoderWAV(m_MixerChannel);
                    break;
                case StreamEncoding.Wma:
                    Un4seen.Bass.Misc.EncoderWMA wmaEnc = new Un4seen.Bass.Misc.EncoderWMA(m_MixerChannel);
                    wmaEnc.WMA_Bitrate = (int)Un4seen.Bass.Misc.EncoderWMA.BITRATE.kbps_128;
                    encoder = wmaEnc;
                    break;
                case StreamEncoding.Ogg:
                    Un4seen.Bass.Misc.EncoderOGG oggEnc = new Un4seen.Bass.Misc.EncoderOGG(m_MixerChannel);
                    oggEnc.OGG_Bitrate = (int)Un4seen.Bass.Misc.EncoderOGG.BITRATE.kbps_128;
                    oggEnc.OGG_TargetSampleRate = (int)Un4seen.Bass.Misc.EncoderOGG.SAMPLERATE.Hz_44100;
                    oggEnc.OGG_UseQualityMode = true;
                    encoder = oggEnc;
                    break;
                case StreamEncoding.Lame:
                    Un4seen.Bass.Misc.EncoderLAME lameEnc = new Un4seen.Bass.Misc.EncoderLAME(m_MixerChannel);
                    lameEnc.LAME_Bitrate = (int)Un4seen.Bass.Misc.EncoderLAME.BITRATE.kbps_128;
                    lameEnc.LAME_Mode = Un4seen.Bass.Misc.EncoderLAME.LAMEMode.Stereo;
                    lameEnc.LAME_TargetSampleRate = (int)Un4seen.Bass.Misc.EncoderLAME.SAMPLERATE.Hz_44100;
                    lameEnc.LAME_Quality = Un4seen.Bass.Misc.EncoderLAME.LAMEQuality.Quality;
                    encoder = lameEnc;
                    break;
                default:
                    Un4seen.Bass.Bass.BASS_StreamFree(m_MixerChannel);
                    m_MixerChannel = 0;
                    return;
            }
            encoder.InputFile = null;
            encoder.OutputFile = null;
            Un4seen.Bass.Misc.IStreamingServer server = null;
            switch (parameters.Streamer)
            {
                case StreamerType.Icecast:
                    {
                        Un4seen.Bass.Misc.ICEcast iceCast = new Un4seen.Bass.Misc.ICEcast(encoder, true);
                        iceCast.Username = parameters.Username;
                        iceCast.Password = parameters.Password;
                        iceCast.ServerAddress = parameters.ServerAddress;
                        iceCast.ServerPort = parameters.ServerPort;
                        iceCast.StreamName = parameters.StreamName;
                        iceCast.PublicFlag = false;
                        iceCast.MountPoint = parameters.Encoding == StreamEncoding.Ogg ? "/Ares.ogg" : "/Ares";
                        server = iceCast;
                        break;
                    }
                case StreamerType.Shoutcast:
                    {
                        Un4seen.Bass.Misc.SHOUTcast shoutCast = new Un4seen.Bass.Misc.SHOUTcast(encoder, true);
#if !MEDIA_PORTAL
                        shoutCast.Username = parameters.Username;
#endif
                        shoutCast.Password = parameters.Password;
                        shoutCast.ServerAddress = parameters.ServerAddress;
                        shoutCast.ServerPort = parameters.ServerPort;
                        shoutCast.StationName = parameters.StreamName;
                        shoutCast.PublicFlag = false;
                        server = shoutCast;
                        break;
                    }
                default:
                    Un4seen.Bass.Bass.BASS_StreamFree(m_MixerChannel);
                    m_MixerChannel = 0;
                    return;
            }
            m_BroadCast = new Un4seen.Bass.Misc.BroadCast(server);
            m_BroadCast.AutoReconnect = true;
            m_BroadCast.Notification += new Un4seen.Bass.Misc.BroadCastEventHandler(m_BroadCast_Notification);
            m_BroadCast.AutoConnect();
            if (!Un4seen.Bass.Bass.BASS_ChannelPlay(m_MixerChannel, false))
            {
                HandleBassError();
            }
            else
            {
                IsStreaming = true;
            }
        }

        void m_BroadCast_Notification(object sender, Un4seen.Bass.Misc.BroadCastEventArgs e)
        {
            if (m_BroadCast == null)
                return;
            if (m_BroadCast.IsConnected)
            {
                // ErrorHandling.ErrorOccurred(0, "Connected to Icecast");
            }
            else
            {
                // ErrorHandling.ErrorOccurred(0, "Disconnected from Icecast");
            }
        }

        public void EndStreaming()
        {
            if (!IsStreaming)
                return;
            if (m_BroadCast != null)
                m_BroadCast.Disconnect();
            m_BroadCast = null;
            if (m_MixerChannel != 0)
                Un4seen.Bass.Bass.BASS_StreamFree(m_MixerChannel);
            m_MixerChannel = 0;
            IsStreaming = false;
        }

        public bool IsStreaming { get; set; }
        
        public bool AddChannel(int channel)
        {
            if (!IsStreaming)
                return false;
            bool result = Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(m_MixerChannel, channel, Un4seen.Bass.BASSFlag.BASS_MIXER_DOWNMIX | Un4seen.Bass.BASSFlag.BASS_MIXER_NORAMPIN | Un4seen.Bass.BASSFlag.BASS_STREAM_AUTOFREE);
            if (!result)
                HandleBassError();
            return result;
        }

        public void RemoveChannel(int channel)
        {
            if (!IsStreaming)
                return;

            bool result = Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelRemove(channel);
        }

        private void HandleBassError()
        {
            ErrorHandling.BassErrorOccurred(0, StringResources.StreamingError);
        }

        public static BassStreamer Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new BassStreamer();
                    /*
                    StreamingParameters streamingParams = new StreamingParameters();
                    streamingParams.StreamName = "Ares";
                    streamingParams.ServerPort = 8000;
                    streamingParams.Password = "ares";
                    streamingParams.ServerAddress = "192.168.2.104";
                    streamingParams.Username = "";
                    streamingParams.Streamer = StreamerType.Icecast;
                    streamingParams.Encoding = StreamEncoding.Ogg;
                    s_Instance.BeginStreaming(streamingParams);
                     */
                }

                return s_Instance;
            }
        }

        private int m_MixerChannel;
        private Un4seen.Bass.Misc.BroadCast m_BroadCast;
        private static BassStreamer s_Instance;
    }
}