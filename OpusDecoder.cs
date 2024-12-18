﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POpusCodec.Enums;

namespace POpusCodec
{
    public class OpusDecoder : IDisposable
    {
        public const int MaxFrameSize = 5760;

        private IntPtr _handle = IntPtr.Zero;
        private bool _previousPacketInvalid = false;
        private int _channelCount = 2;

        private Bandwidth? _previousPacketBandwidth = null;

        public Bandwidth? PreviousPacketBandwidth
        {
            get
            {
                return _previousPacketBandwidth;
            }
        }

        public OpusDecoder(SamplingRate outputSamplingRateHz, Channels numChannels)
        {
            if ((outputSamplingRateHz != SamplingRate.Sampling08000)
                && (outputSamplingRateHz != SamplingRate.Sampling12000)
                && (outputSamplingRateHz != SamplingRate.Sampling16000)
                && (outputSamplingRateHz != SamplingRate.Sampling24000)
                && (outputSamplingRateHz != SamplingRate.Sampling48000))
            {
                throw new ArgumentOutOfRangeException("outputSamplingRateHz", "Must use one of the pre-defined sampling rates");
            }
            if ((numChannels != Channels.Mono)
                && (numChannels != Channels.Stereo))
            {
                throw new ArgumentOutOfRangeException("numChannels", "Must be Mono or Stereo");
            }

            _channelCount = (int)numChannels;
            _handle = Wrapper.opus_decoder_create(outputSamplingRateHz, numChannels);

            if (_handle == IntPtr.Zero)
            {
                throw new OpusException(OpusStatusCode.AllocFail, "Memory was not allocated for the encoder");
            }
        }

        public short[] DecodePacketLost()
        {
            _previousPacketInvalid = true;

            short[] tempData = new short[MaxFrameSize * _channelCount];

            int numSamplesDecoded = Wrapper.opus_decode(_handle, null, tempData, 0, _channelCount);

            if (numSamplesDecoded == 0)
                return new short[] { };

            short[] pcm = new short[numSamplesDecoded * _channelCount];
            Buffer.BlockCopy(tempData, 0, pcm, 0, pcm.Length * sizeof(short));

            return pcm;
        }

        public int DecodePacketLostFloat(float[] decodedSamples, int count)
        {
            _previousPacketInvalid = true;

            int numSamplesDecoded = Wrapper.opus_decode(_handle, null, 0, decodedSamples, 0, _channelCount, count);

            return numSamplesDecoded;
        }

        public float[] DecodePacketLostFloat()
        {
            _previousPacketInvalid = true;

            float[] tempData = new float[MaxFrameSize * _channelCount];

            int numSamplesDecoded = Wrapper.opus_decode(_handle, null, 0, tempData, 0, _channelCount);

            if (numSamplesDecoded == 0)
                return new float[] { };

            float[] pcm = new float[numSamplesDecoded * _channelCount];
            Buffer.BlockCopy(tempData, 0, pcm, 0, pcm.Length * sizeof(float));

            return pcm;
        }


        public short[] DecodePacket(byte[] packetData)
        {
            short[] tempData = new short[MaxFrameSize * _channelCount];

            int bandwidth = Wrapper.opus_packet_get_bandwidth(packetData);

            int numSamplesDecoded = 0;

            if (bandwidth == (int)OpusStatusCode.InvalidPacket)
            {
                numSamplesDecoded = Wrapper.opus_decode(_handle, null, tempData, 0, _channelCount);
                _previousPacketInvalid = true;
            }
            else
            {
                _previousPacketBandwidth = (Bandwidth)bandwidth;
                numSamplesDecoded = Wrapper.opus_decode(_handle, packetData, tempData, _previousPacketInvalid ? 1 : 0, _channelCount);
                
                _previousPacketInvalid = false;
            }

            if (numSamplesDecoded == 0)
                return new short[] { };

            short[] pcm = new short[numSamplesDecoded * _channelCount];
            Buffer.BlockCopy(tempData, 0, pcm, 0, pcm.Length * sizeof(short));

            return pcm;
        }

        public int DecodePacketFloat(byte[] packetData, int count, float[] decodedSamples)
        {
            int bandwidth = Wrapper.opus_packet_get_bandwidth(packetData);

            int numSamplesDecoded = 0;

            if (bandwidth == (int)OpusStatusCode.InvalidPacket)
            {
                numSamplesDecoded = Wrapper.opus_decode(_handle, null, 0, decodedSamples, 0, _channelCount);
                _previousPacketInvalid = true;
            }
            else
            {
                _previousPacketBandwidth = (Bandwidth)bandwidth;
                numSamplesDecoded = Wrapper.opus_decode(_handle, packetData, count, decodedSamples, _previousPacketInvalid ? 1 : 0, _channelCount);

                _previousPacketInvalid = false;
            }

            return numSamplesDecoded;
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                Wrapper.opus_decoder_destroy(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
