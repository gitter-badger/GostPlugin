﻿using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace GostPlugin
{
    public sealed class GostCryptoTransform : ICryptoTransform
    {
        private readonly byte[] _key = new byte[GostECB.KeyLength];
        private byte[] _state = new byte[GostECB.BlockSize];
        private bool _encrypt;

        /// <summary>
        /// Creates instance of GOST cipher transform.
        /// </summary>
        /// <param name="_key">256-bit _key</param>
        /// <param name="_state">Initialization vector</param>
        /// <param name="_encrypt">Use True for encryption mode</param>
        public GostCryptoTransform (byte[] key, byte[] iv, bool encrypt)
        {
            Debug.Assert(key.Length == GostECB.KeyLength, "Key must be 256-bit long");
            Debug.Assert(iv.Length == GostECB.KeyLength, "Initialization Vector must be 64-bit long");

            Array.Copy(key, _key, GostECB.KeyLength);
            Array.Copy(iv, _state, GostECB.BlockSize);
            _encrypt = encrypt;
        }

        /// <summary>
        /// This module implements so-called Cipher Feedback Mode of GOST algorithm which can be
        /// used only to process one set of data - to either _encrypt a signle plaintext or decrypt
        /// a single ciphertext.
        /// </summary>
        public bool CanReuseTransform { get { return false; } }

        /// <summary>
        /// This property is always false, because the implementation can handle only 64-bit blocks.
        /// </summary>
        public bool CanTransformMultipleBlocks { get { return false; } }

        /// <summary>
        /// Input block size is always 64-bit.
        /// </summary>
        public int InputBlockSize { get { return GostECB.BlockSize; } }

        /// <summary>
        /// Input block size is always 64-bit.
        /// </summary>
        public int OutputBlockSize { get { return GostECB.BlockSize; } }

        /// <summary>
        /// Performs cryptographic transform of a single (next) 64-bit block
        /// </summary>
        /// <param name="inputBuffer"></param>
        /// <param name="inputOffset"></param>
        /// <param name="inputCount">Must be 8 bytes (64 bits)</param>
        /// <param name="outputBuffer"></param>
        /// <param name="outputOffset"></param>
        /// <returns></returns>
        public int TransformBlock (byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputCount != 0)
            {
                byte[] dataBlock = new byte[inputCount];
                byte[] gamma = new byte[GostECB.BlockSize];
                byte[] result = new byte[inputCount];

                Array.Copy(inputBuffer, inputOffset, dataBlock, 0, inputCount);

                gamma = GostECB.Process(_state, _key, GostECB.SBox_CryptoPro_A, true);
                result = XOr(dataBlock, gamma);

                Array.Copy(result, 0, outputBuffer, outputOffset, inputCount);
                Array.Copy(_encrypt ? result : dataBlock, _state, inputCount);
            }

            return inputCount;
        }

        private byte[] XOr (byte[] a, byte[] b)
        {
            Debug.Assert(a.Length <= b.Length, "Block length must be shorter or equal to gamma");

            var c = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                c[i] = (byte)(a[i] ^ b[i]);
            }
            return c;
        }

        /// <summary>
        /// Processing of the last block does not differ from previous ones, so we can just call
        /// TransformBlock once again.
        /// </summary>
        /// <param name="inputBuffer"></param>
        /// <param name="inputOffset"></param>
        /// <param name="inputCount"></param>
        /// <returns></returns>
        public byte[] TransformFinalBlock (byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var outputBuffer = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, 0);
            return outputBuffer;
        }

        public void Dispose ()
        {
        }
    }
}