using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Utilities;
using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;

namespace BaileysCSharp.LibSignal
{
    public class NodeCrypto
    {
        public static KeyPair GenerateKeyPair()
        {
            var x25519KeyPairGenerator = GeneratorUtilities.GetKeyPairGenerator("X25519");
            x25519KeyPairGenerator.Init(new X25519KeyGenerationParameters(new SecureRandom()));

            AsymmetricCipherKeyPair keyPair = x25519KeyPairGenerator.GenerateKeyPair();

            var publicKeyBytes = ((X25519PublicKeyParameters)keyPair.Public).GetEncoded();
            var privateKeyBytes = ((X25519PrivateKeyParameters)keyPair.Private).GetEncoded();

            var buffer = new byte[publicKeyBytes.Length + 1];
            buffer[0] = 5;
            publicKeyBytes.CopyTo(buffer, 1);

            return new KeyPair()
            {
                Public = buffer,
                Private = privateKeyBytes,
            };
        }
    }

    public class Curve
    {
        public static long[] X = new long[] { 0xd51a, 0x8f25, 0x2d60, 0xc956, 0xa7b2, 0x9525, 0xc760, 0x692c, 0xdc5c, 0xfdd6, 0xe231, 0xc0a4, 0x53fe, 0xcd6e, 0x36d3, 0x2169, };
        public static long[] Y = new long[] { 0x6658, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, 0x6666, };
        public static long[] D2 = new long[] { 0xf159, 0x26b2, 0x9b94, 0xebd6, 0xb156, 0x8283, 0x149a, 0x00e0, 0xd130, 0xeef3, 0x80f2, 0x198e, 0xfce7, 0x56df, 0xd9dc, 0x2406 };

        private static Byte[] iv = new Byte[64]
        {
          0x6a,0x09,0xe6,0x67,0xf3,0xbc,0xc9,0x08,
          0xbb,0x67,0xae,0x85,0x84,0xca,0xa7,0x3b,
          0x3c,0x6e,0xf3,0x72,0xfe,0x94,0xf8,0x2b,
          0xa5,0x4f,0xf5,0x3a,0x5f,0x1d,0x36,0xf1,
          0x51,0x0e,0x52,0x7f,0xad,0xe6,0x82,0xd1,
          0x9b,0x05,0x68,0x8c,0x2b,0x3e,0x6c,0x1f,
          0x1f,0x83,0xd9,0xab,0xfb,0x41,0xbd,0x6b,
          0x5b,0xe0,0xcd,0x19,0x13,0x7e,0x21,0x79
        };



        private static long[] K = new long[]
{
    1116352408,
  3609767458,
  1899447441,
  602891725,
  3049323471,
  3964484399,
  3921009573,
  2173295548,
  961987163,
  4081628472,
  1508970993,
  3053834265,
  2453635748,
  2937671579,
  2870763221,
  3664609560,
  3624381080,
  2734883394,
  310598401,
  1164996542,
  607225278,
  1323610764,
  1426881987,
  3590304994,
  1925078388,
  4068182383,
  2162078206,
  991336113,
  2614888103,
  633803317,
  3248222580,
  3479774868,
  3835390401,
  2666613458,
  4022224774,
  944711139,
  264347078,
  2341262773,
  604807628,
  2007800933,
  770255983,
  1495990901,
  1249150122,
  1856431235,
  1555081692,
  3175218132,
  1996064986,
  2198950837,
  2554220882,
  3999719339,
  2821834349,
  766784016,
  2952996808,
  2566594879,
  3210313671,
  3203337956,
  3336571891,
  1034457026,
  3584528711,
  2466948901,
  113926993,
  3758326383,
  338241895,
  168717936,
  666307205,
  1188179964,
  773529912,
  1546045734,
  1294757372,
  1522805485,
  1396182291,
  2643833823,
  1695183700,
  2343527390,
  1986661051,
  1014477480,
  2177026350,
  1206759142,
  2456956037,
  344077627,
  2730485921,
  1290863460,
  2820302411,
  3158454273,
  3259730800,
  3505952657,
  3345764771,
  106217008,
  3516065817,
  3606008344,
  3600352804,
  1432725776,
  4094571909,
  1467031594,
  275423344,
  851169720,
  430227734,
  3100823752,
  506948616,
  1363258195,
  659060556,
  3750685593,
  883997877,
  3785050280,
  958139571,
  3318307427,
  1322822218,
  3812723403,
  1537002063,
  2003034995,
  1747873779,
  3602036899,
  1955562222,
  1575990012,
  2024104815,
  1125592928,
  2227730452,
  2716904306,
  2361852424,
  442776044,
  2428436474,
  593698344,
  2756734187,
  3733110249,
  3204031479,
  2999351573,
  3329325298,
  3815920427,
  3391569614,
  3928383900,
  3515267271,
  566280711,
  3940187606,
  3454069534,
  4118630271,
  4000239992,
  116418474,
  1914138554,
  174292421,
  2731055270,
  289380356,
  3203993006,
  460393269,
  320620315,
  685471733,
  587496836,
  852142971,
  1086792851,
  1017036298,
  365543100,
  1126000580,
  2618297676,
  1288033470,
  3409855158,
  1501505948,
  4234509866,
  1607167915,
  987167468,
  1816402316,
  1246189591,
};
        private static Int64[] L = new Int64[32]
        {
            0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58,
            0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0x10
        };
        private static Int64[] I = new Int64[] { 0xa0b0, 0x4a0e, 0x1b27, 0xc4ee, 0xe478, 0xad2f, 0x1806, 0x2f43, 0xd7a7, 0x3dfb, 0x0099, 0x2b4d, 0xdf0b, 0x4fc1, 0x2480, 0x2b83 };
        private static Int64[] D = new Int64[] { 0x78a3, 0x1359, 0x4dca, 0x75eb, 0xd8ab, 0x4141, 0x0a4d, 0x0070, 0xe898, 0x7779, 0x4079, 0x8cc7, 0xfe73, 0x2b6f, 0x6cee, 0x5203 };

        public static KeyPair GenerateKeyPair()
        {
            var x25519KeyPairGenerator = GeneratorUtilities.GetKeyPairGenerator("X25519");
            x25519KeyPairGenerator.Init(new X25519KeyGenerationParameters(new SecureRandom()));

            AsymmetricCipherKeyPair keyPair = x25519KeyPairGenerator.GenerateKeyPair();

            var publicKeyBytes = ((X25519PublicKeyParameters)keyPair.Public).GetEncoded();
            var privateKeyBytes = ((X25519PrivateKeyParameters)keyPair.Private).GetEncoded();

            return new KeyPair()
            {
                Public = publicKeyBytes,
                Private = privateKeyBytes,
            };
        }


        internal static bool VerifySignature(byte[] publicKey, byte[] message, byte[] signature)
        {
            if (publicKey.Length == 33)
            {
                publicKey = publicKey.Skip(1).ToArray();
            }
            if (signature.Length != 64)
            {
                return false;
            }
            if (publicKey.Length != 32)
            {
                return false;
            }

            return Verify(publicKey, message, signature);
        }

        internal static bool Verify(byte[] publicKey, byte[] message, byte[] signature)
        {
            var sm = new byte[64 + message.Length];
            var m = new byte[64 + message.Length];
            for (var i = 0; i < 64; i++)
                sm[i] = signature[i];
            for (var i = 0; i < message.Length; i++)
                sm[i + 64] = message[i];


            return curve25519_sign_open(m, sm, publicKey) >= 0;
        }

        private static int curve25519_sign_open(byte[] m, byte[] sm, byte[] pk)
        {
            // Convert Curve25519 public key into Ed25519 public key.
            var edpk = convertPublicKey(pk);
            // Restore sign bit from signature.
            edpk[31] |= (byte)(sm[63] & 128);
            // Remove sign bit from signature.
            sm[63] &= 127;
            // Verify signed message.

            return crypto_sign_open(m, sm, edpk);
        }

        private static byte[] convertPublicKey(byte[] pk)
        {
            var z = new byte[32];
            var x = GF();
            var a = GF();
            var b = GF();

            Unpack25519(x, pk);
            A(a, x, GF(1));
            Z(b, x, GF(1));
            Inv25519(a, a);
            M(a, a, b);
            Pack25519(z, a);
            return z;
        }


        private static void Unpack25519(Int64[] /*gf*/ o, Byte[] n)
        {
            for (var i = 0; i < 16; ++i)
            {
                o[i] = (0xff & n[2 * i]) + ((0xffL & n[2 * i + 1]) << 8);
            }

            o[15] &= 0x7fff;
        }

        public static byte[] Sign(byte[] secretKey, byte[] message)
        {
            byte[] resultingBuffer = new byte[64 + message.Length];
            curve25519_sign(resultingBuffer, message, message.Length, secretKey);
            byte[] signature = new byte[64];
            for (var i = 0; i < signature.Length; i++)
                signature[i] = resultingBuffer[i];
            return signature;
        }


        #region inner magic 



        private static int crypto_sign_open(byte[] m, byte[] sm, byte[] pk)
        {

            Byte[] t = new Byte[32];
            Byte[] h = new Byte[64];
            var p = new long[][] { GF(), GF(), GF(), GF() };
            var q = new long[][] { GF(), GF(), GF(), GF() };

            Int32 messageSize = sm.Length;
            if (messageSize < 64)
                return -1;
            if (Unpackneg(q, pk) != 0)
                return -1;
            for (var i = 0; i < sm.Length; i++)
                m[i] = sm[i];
            for (var i = 0; i < 32; i++)
                m[i + 32] = pk[i];

            CryptoHash(h, m, sm.Length);
            Reduce(h);
            Scalarmult(p, q, h);

            var b64 = string.Join(",", sm);

            var subArray = sm.Skip(32).ToArray();
            Scalarbase(q, subArray);
            Add(p, q);
            Pack(t, p);
            messageSize -= 64;
            if (CryptoVerify32(sm, t) != 0)
            {
                for (var i = 0; i < sm.Length; i++)
                    m[i] = 0;
                return -1;
            }

            for (var i = 0; i < messageSize; i++)
                m[i] = sm[i + 64];

            return 0;
        }

        private static int CryptoVerify32(byte[] x, byte[] y)
        {
            return Vn(x, y, 32);
        }

        private static Int32 Vn(Byte[] x, Byte[] y, Int32 n, Int32 xOffset = 0)
        {
            Int32 d = 0;
            for (var i = 0; i < n; ++i) d |= x[i + xOffset] ^ y[i];
            return (1 & ((d - 1) >> 8)) - 1;
        }

        private static int Unpackneg(long[][] r, byte[] p)
        {
            var t = GF();
            var chk = GF();
            var num = GF();
            var den = GF();
            var den2 = GF();
            var den4 = GF();
            var den6 = GF();

            Set25519(r[2], GF(1));
            Unpack25519(r[1], p);
            S(num, r[1]);
            M(den, num, D);
            Z(num, num, r[2]);
            A(den, r[2], den);
            S(den2, den);
            S(den4, den2);
            M(den6, den4, den2);
            M(t, den6, num);
            M(t, t, den);
            Pow2523(t, t);
            M(t, t, num);
            M(t, t, den);
            M(t, t, den);
            M(r[0], t, den);
            S(chk, r[0]);
            M(chk, chk, den);
            if (Neq25519(chk, num) != 0)
                M(r[0], r[0], I);
            S(chk, r[0]);
            M(chk, chk, den);
            if (Neq25519(chk, num) != 0)
                return -1;
            if (Par25519(r[0]) == p[31] >> 7)
                Z(r[0], GF(), r[0]);
            M(r[3], r[0], r[1]);
            return 0;

        }


        private static Int32 Neq25519(Int64[] /*gf*/ a, Int64[] /*gf*/ b)
        {
            Byte[] c = new Byte[32], d = new Byte[32];
            Pack25519(c, a);
            Pack25519(d, b);
            return CryptoVerify32(c, d);
        }

        private static void Pow2523(Int64[] /*gf*/ o, Int64[] /*gf*/ i)
        {
            Int64[] /*gf*/ c = GF();

            for (var a = 0; a < 16; ++a)
            {
                c[a] = i[a];
            }

            for (var a = 250; a >= 0; a--)
            {
                S(c, c);

                if (a != 1)
                {
                    M(c, c, i);
                }
            }

            for (var a = 0; a < 16; ++a)
            {
                o[a] = c[a];
            }
        }


        private static int curve25519_sign(byte[] sm, byte[] m, int n, byte[] sk)
        {
            // If opt_rnd is provided, sm must have n + 128,
            // otherwise it must have n + 64 bytes.
            // Convert Curve25519 secret key into Ed25519 secret key (includes pub key).
            var edsk = new byte[64];
            var p = new long[][] { GF(), GF(), GF(), GF() };
            for (var i = 0; i < 32; i++)
                edsk[i] = sk[i];
            // Ensure private key is in the correct format.
            edsk[0] &= 248;
            edsk[31] &= 127;
            edsk[31] |= 64;
            Scalarbase(p, edsk);
            var subArray = edsk.Take(32).ToArray();
            Pack(subArray, p);
            for (int i = 0; i < subArray.Length; i++)
            {
                edsk[32 + i] = subArray[i];
            }
            var signBit = edsk[63] & 128;

            var smlen = crypto_sign_direct(sm, m, n, edsk);
            sm[63] |= (byte)signBit;
            return smlen;
        }



        private static int crypto_sign_direct(byte[] sm, byte[] m, int n, byte[] sk)
        {
            var h = new byte[64];
            var r = new byte[64];
            var p = new long[][] { GF(), GF(), GF(), GF() };

            long[] x = new long[64];

            for (var i = 0; i < n; i++)
                sm[64 + i] = m[i];
            for (var i = 0; i < 32; i++)
                sm[32 + i] = (byte)sk[i];

            CryptoHash(r, sm.SubArray(32, sm.Length - 32), n + 32);
            Reduce(r);
            Scalarbase(p, r);
            Pack(sm, p);
            for (var i = 0; i < 32; i++)
                sm[i + 32] = sk[32 + i];

            CryptoHash(h, sm, n + 64);
            Reduce(h);
            for (var i = 0; i < 64; i++)
                x[i] = 0;
            for (var i = 0; i < 32; i++)
                x[i] = r[i];
            for (var i = 0; i < 32; i++)
            {
                for (var j = 0; j < 32; j++)
                {
                    x[i + j] += h[i] * sk[j];
                }
            }
            var subArray = sm.SubArray(32, sm.Length - 32);
            ModL(subArray, x);
            for (int i = 0; i < subArray.Length; i++)
            {
                sm[32 + i] = subArray[i];
            }
            return n + 64;
        }

        private static void Reduce(Byte[] r)
        {
            Int64[] x = new Int64[64];
            for (int i = 0; i < 64; i++)
            {
                x[i] = 0xff & r[i];
            }

            for (int i = 0; i < 64; ++i)
            {
                r[i] = 0;
            }

            ModL(r, x);
        }

        private static void ModL(Byte[] r, Int64[] x/*[64]*/)
        {
            Int64 carry;
            Int32 i, j;
            for (i = 63; i >= 32; --i)
            {
                carry = 0;
                for (j = i - 32; j < i - 12; ++j)
                {
                    x[j] += carry - 16 * x[i] * L[j - (i - 32)];
                    carry = (x[j] + 128) >> 8;
                    x[j] -= carry << 8;
                }
                x[j] += carry;
                x[i] = 0;
            }
            carry = 0;

            for (j = 0; j < 32; ++j)
            {
                x[j] += carry - (x[31] >> 4) * L[j];
                carry = x[j] >> 8;
                x[j] &= 255;
            }

            for (j = 0; j < 32; ++j)
            {
                x[j] -= carry * L[j];
            }

            for (i = 0; i < 32; ++i)
            {
                x[i + 1] += x[i] >> 8;
                r[i] = (Byte)(x[i] & 255);
            }
        }

        public static Int32 CryptoHash(byte[] hash, byte[] message, Int32 n)
        {
            Int64[] @in = new Int64[message.Length];
            for (int i = 0; i < message.Length; i++)
            {
                @in[i] = message[i];
            }
            return CryptoHash(hash, @in, n);
        }

        public static Int32 CryptoHash(byte[] hash, Int64[] message, Int32 n)
        {
            Int64[] @out = new Int64[hash.Length];
            int[] hh = new int[] { 1779033703, -1150833019, 1013904242, -1521486534, 1359893119, -1694144372, 528734635, 1541459225 };
            int[] hl = new int[] { -205731576, -2067093701, -23791573, 1595750129, -1377402159, 725511199, -79577749, 327033209 };
            long[] x = new long[256];
            var b = n;
            CryptoHashBlocksHl(hh, hl, message, n);
            n %= 128;
            for (var i = 0; i < n; i++)
                x[i] = message[b - n + i];
            x[n] = 128;
            n = 256 - 128 * (n < 112 ? 1 : 0);
            x[n - 9] = 0;
            Ts64(x, n - 8, (b / 0x20000000) | 0, b << 3);
            CryptoHashBlocksHl(hh, hl, x, n);
            for (var i = 0; i < 8; i++)
                Ts64(@out, 8 * i, hh[i], hl[i]);

            for (int i = 0; i < @out.Length; i++)
            {
                hash[i] = (byte)@out[i];
            }

            return 0;




        }

        private static int CryptoHashBlocksHl(int[] hh, int[] hl, Int64[] m, int n)
        {
            var wh = new int[16];
            var wl = new int[16];

            var ah = new int[8];
            Array.Copy(hh, ah, ah.Length);
            var al = new int[8];
            Array.Copy(hl, al, al.Length);
            var bh = new int[8];
            var bl = new int[8];
            int pos = 0;
            long h, l, a, b, c, d, th, tl;

            while (n >= 128)
            {
                for (var i = 0; i < 16; i++)
                {
                    int j = 8 * i + pos;
                    wh[i] = unchecked((int)((m[j + 0] << 24) | (m[j + 1] << 16) | (m[j + 2] << 8) | m[j + 3]));
                    wl[i] = unchecked((int)((m[j + 4] << 24) | (m[j + 5] << 16) | (m[j + 6] << 8) | m[j + 7]));
                }

                //Working
                for (var i = 0; i < 80; i++)
                {
                    Array.Copy(ah, bh, bh.Length);
                    Array.Copy(al, bl, bl.Length);

                    // Add - Working
                    h = ah[7];
                    l = al[7];
                    a = l & 0xffff;
                    b = ShiftR16(l);
                    c = h & 0xffff;
                    d = ShiftR16(h);

                    // Sigma1 - Working
                    h = ((ah[4] >>> 14) | (al[4] << (32 - 14))) ^ ((ah[4] >>> 18) | (al[4] << (32 - 18))) ^ ((al[4] >>> (41 - 32)) | (ah[4] << (32 - (41 - 32))));
                    l = ((al[4] >>> 14) | (ah[4] << (32 - 14))) ^ ((al[4] >>> 18) | (ah[4] << (32 - 18))) ^ ((ah[4] >>> (41 - 32)) | (al[4] << (32 - (41 - 32))));

                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);

                    // Ch - Working
                    h = (ah[4] & ah[5]) ^ (~ah[4] & ah[6]);
                    l = (al[4] & al[5]) ^ (~al[4] & al[6]);

                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);

                    // K - Working
                    h = (int)K[i * 2];
                    l = (int)K[i * 2 + 1];

                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);

                    // w - Working
                    h = wh[i % 16];
                    l = wl[i % 16];

                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);

                    b += ShiftR16(a);
                    c += ShiftR16(b);
                    d += ShiftR16(c);
                    th = ((int)c & 0xffff) | ((int)d << 16);
                    tl = ((int)a & 0xffff) | ((int)b << 16);


                    // add - Working
                    h = th;
                    l = tl;
                    a = l & 0xffff;
                    b = ShiftR16(l);
                    c = h & 0xffff;
                    d = ShiftR16(h);

                    // Sigma0 - Working
                    h = ((ah[0] >>> 28) | (al[0] << (32 - 28))) ^ ((al[0] >>> (34 - 32)) | (ah[0] << (32 - (34 - 32)))) ^ ((al[0] >>> (39 - 32)) | (ah[0] << (32 - (39 - 32))));
                    l = ((al[0] >>> 28) | (ah[0] << (32 - 28))) ^ ((ah[0] >>> (34 - 32)) | (al[0] << (32 - (34 - 32)))) ^ ((ah[0] >>> (39 - 32)) | (al[0] << (32 - (39 - 32))));
                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);

                    // Maj - Working
                    h = (ah[0] & ah[1]) ^ (ah[0] & ah[2]) ^ (ah[1] & ah[2]);
                    l = (al[0] & al[1]) ^ (al[0] & al[2]) ^ (al[1] & al[2]);
                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);
                    b += ShiftR16(a);
                    c += ShiftR16(b);
                    d += ShiftR16(c);
                    bh[7] = ((int)c & 0xffff) | ((int)d << 16);
                    bl[7] = ((int)a & 0xffff) | ((int)b << 16);

                    // add - Working
                    h = bh[3];
                    l = bl[3];
                    a = l & 0xffff;
                    b = ShiftR16(l);
                    c = h & 0xffff;
                    d = ShiftR16(h);
                    h = th;
                    l = tl;
                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);
                    b += ShiftR16(a);
                    c += ShiftR16(b);
                    d += ShiftR16(c);
                    bh[3] = ((int)c & 0xffff) | ((int)d << 16);
                    bl[3] = ((int)a & 0xffff) | ((int)b << 16);

                    //Copy Back - Working
                    for (int j = 1; j <= 7; j++)
                    {
                        ah[j] = bh[j - 1];
                        al[j] = bl[j - 1];
                    }
                    ah[0] = bh[7];
                    al[0] = bl[7];

                    if (i % 16 == 15)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            // add - Working
                            h = wh[j];
                            l = wl[j];
                            a = l & 0xffff;
                            b = ShiftR16(l);
                            c = h & 0xffff;
                            d = ShiftR16(h);
                            h = wh[(j + 9) % 16];
                            l = wl[(j + 9) % 16];
                            a += l & 0xffff;
                            b += ShiftR16(l);
                            c += h & 0xffff;
                            d += ShiftR16(h);

                            // sigma0 - Working
                            th = wh[(j + 1) % 16];
                            tl = wl[(j + 1) % 16];
                            h = (((int)th >>> 1) | ((int)tl << (32 - 1))) ^ (((int)th >>> 8) | ((int)tl << (32 - 8))) ^ ((int)th >>> 7);
                            l = (((int)tl >>> 1) | ((int)th << (32 - 1))) ^ (((int)tl >>> 8) | ((int)th << (32 - 8))) ^ (((int)tl >>> 7) | ((int)th << (32 - 7)));
                            a += l & 0xffff;
                            b += ShiftR16(l);
                            c += h & 0xffff;
                            d += ShiftR16(h);

                            // sigma1 - Working
                            th = wh[(j + 14) % 16];
                            tl = wl[(j + 14) % 16];
                            h = (((int)th >>> 19) | ((int)tl << (32 - 19))) ^ (((int)tl >>> (61 - 32)) | ((int)th << (32 - (61 - 32)))) ^ ((int)th >>> 6);
                            l = (((int)tl >>> 19) | ((int)th << (32 - 19))) ^ (((int)th >>> (61 - 32)) | ((int)tl << (32 - (61 - 32)))) ^ (((int)tl >>> 6) | ((int)th << (32 - 6)));
                            a += l & 0xffff;
                            b += ShiftR16(l);
                            c += h & 0xffff;
                            d += ShiftR16(h);
                            b += ShiftR16(a);
                            c += ShiftR16(b);
                            d += ShiftR16(c);
                            wh[j] = ((int)c & 0xffff) | ((int)d << 16);
                            wl[j] = ((int)a & 0xffff) | ((int)b << 16);

                        }
                    }
                }

                for (int i = 0; i < 8; i++)
                {
                    h = ah[i];
                    l = al[i];
                    a = l & 0xffff;
                    b = ShiftR16(l);
                    c = h & 0xffff;
                    d = ShiftR16(h);
                    h = hh[i];
                    l = hl[i];
                    a += l & 0xffff;
                    b += ShiftR16(l);
                    c += h & 0xffff;
                    d += ShiftR16(h);
                    b += ShiftR16(a);
                    c += ShiftR16(b);
                    d += ShiftR16(c);
                    hh[i] = ah[i] = ((int)c & 0xffff) | ((int)d << 16);
                    hl[i] = al[i] = ((int)a & 0xffff) | ((int)b << 16);
                }
                pos += 128;
                n -= 128;
            }
            return n;
        }

        public static long ShiftR16(long value)
        {
            return ShiftN(value, 16);
        }
        public static long ShiftN(long value, int n)
        {
            return (value >>> n) & 0xFFFF;
        }

        private static void Ts64(Int64[] x, int i, int h, Int32 l)
        {
            x[i] = (h >> 24) & 0xff;
            x[i + 1] = ((h >> 16) & 0xff);
            x[i + 2] = ((h >> 8) & 0xff);
            x[i + 3] = (h & 0xff);
            x[i + 4] = ((l >> 24) & 0xff);
            x[i + 5] = ((l >> 16) & 0xff);
            x[i + 6] = ((l >> 8) & 0xff);
            x[i + 7] = (l & 0xff);
        }


        private static void Pack(byte[] r, long[][] p)
        {
            long[] tx = GF(), ty = GF(), zi = GF();
            Inv25519(zi, p[2]);
            M(tx, p[0], zi);
            M(ty, p[1], zi);
            Pack25519(r, ty);
            r[31] ^= (Byte)(Par25519(tx) << 7);
        }

        private static long Par25519(long[] a)
        {
            var d = new byte[32];
            Pack25519(d, a);
            return d[0] & 1;
        }

        private static void Pack25519(byte[] o, long[] n)
        {
            long i, j, b;
            long[] m = GF(), t = GF();
            for (i = 0; i < 16; i++)
                t[i] = n[i];
            Car25519(t);
            Car25519(t);
            Car25519(t);
            for (j = 0; j < 2; j++)
            {
                m[0] = t[0] - 0xffed;
                for (i = 1; i < 15; i++)
                {
                    m[i] = t[i] - 0xffff - ((m[i - 1] >> 16) & 1);
                    m[i - 1] &= 0xffff;
                }
                m[15] = t[15] - 0x7fff - ((m[14] >> 16) & 1);
                b = (m[15] >> 16) & 1;
                m[14] &= 0xffff;
                Sel25519(t, m, 1 - b);
            }
            for (i = 0; i < 16; i++)
            {
                o[2 * i] = (Byte)t[i];
                o[2 * i + 1] = (Byte)(t[i] >> 8);
            }
        }

        private static void Inv25519(long[] o, long[] i)
        {
            var c = GF();
            long a;
            for (a = 0; a < 16; a++)
                c[a] = i[a];
            for (a = 253; a >= 0; a--)
            {
                S(c, c);
                if (a != 2 && a != 4)
                    M(c, c, i);
            }
            for (a = 0; a < 16; a++)
                o[a] = c[a];
        }

        private static void S(long[] o, long[] a)
        {
            M(o, a, a);
        }

        private static void Scalarbase(long[][] p, byte[] s)
        {
            var q = new long[][] { GF(), GF(), GF(), GF() };
            Set25519(q[0], X);
            Set25519(q[1], Y);
            Set25519(q[2], GF(1));
            M(q[3], X, Y);
            Scalarmult(p, q, s);
        }

        private static void Scalarmult(long[][] p, long[][] q, byte[] s)
        {
            Set25519(p[0], GF());
            Set25519(p[1], GF(1));
            Set25519(p[2], GF(1));
            Set25519(p[3], GF());
            for (var i = 255; i >= 0; --i)
            {
                Byte b = (Byte)(((0xff & s[i / 8]) >> (i & 7)) & 1);
                Cswap(p, q, b);
                Add(q, p);
                Add(p, p);
                Cswap(p, q, b);
            }
        }

        private static void Add(long[][] p, long[][] q)
        {
            long[] a = GF(), b = GF(), c = GF(), d = GF(), e = GF(), f = GF(), g = GF(), h = GF(), t = GF();

            Z(a, p[1], p[0]);
            Z(t, q[1], q[0]);
            M(a, a, t);
            A(b, p[0], p[1]);
            A(t, q[0], q[1]);
            M(b, b, t);
            M(c, p[3], q[3]);
            M(c, c, D2);
            M(d, p[2], q[2]);
            A(d, d, d);
            Z(e, b, a);
            Z(f, d, c);
            A(g, d, c);
            A(h, b, a);

            M(p[0], e, f);
            M(p[1], h, g);
            M(p[2], g, f);
            M(p[3], e, h);
        }

        private static void A(long[] o, long[] a, long[] b)
        {
            for (var i = 0; i < 16; i++)
                o[i] = a[i] + b[i];
        }

        private static void Z(long[] o, long[] a, long[] b)
        {
            for (var i = 0; i < 16; i++)
                o[i] = a[i] - b[i];
        }

        private static void Cswap(long[][] p, long[][] q, byte b)
        {
            for (var i = 0; i < 4; i++)
                Sel25519(p[i], q[i], b & 0xff);
        }

        private static void Sel25519(long[] p, long[] q, long b)
        {
            Int64 t, c = ~(b - 1);
            for (var i = 0; i < 16; ++i)
            {
                t = c & (p[i] ^ q[i]);
                p[i] ^= t;
                q[i] ^= t;
            }
        }

        private static void M(long[] o, long[] a, long[] b)
        {
            Int64[] t = new Int64[31];

            for (var i = 0; i < 31; ++i)
            {
                t[i] = 0;
            }

            for (var i = 0; i < 16; ++i)
            {
                for (var j = 0; j < 16; ++j)
                {
                    t[i + j] += a[i] * b[j];
                }
            }

            for (var i = 0; i < 15; ++i)
            {
                t[i] += 38 * t[i + 16];
            }

            for (var i = 0; i < 16; ++i)
            {
                o[i] = t[i];
            }

            Car25519(o);
            Car25519(o);
        }


        private static void Car25519(Int64[] /*gf*/ o)
        {
            for (var i = 0; i < 16; ++i)
            {
                o[i] += (1 << 16);
                Int64 c = o[i] >> 16;
                o[(i + 1) * (i < 15 ? 1 : 0)] += c - 1 + 37 * (c - 1) * (i == 15 ? 1 : 0);
                o[i] -= c << 16;
            }
        }

        private static void Set25519(long[] r, long[] a)
        {
            for (var i = 0; i < 16; ++i)
            {
                r[i] = a[i];
            }
        }

        private static long[] GF(params long[] init)
        {
            var buffer = new long[16];
            for (int i = 0; i < init?.Length; i++)
            {
                buffer[i] = init[i];
            }
            return buffer;
        }
        private static long[] GFN(int size, params long[] init)
        {
            var buffer = new long[size];
            for (int i = 0; i < init?.Length; i++)
            {
                buffer[i] = init[i];
            }
            return buffer;
        }


        #endregion



        #region Calculate Agreement

        /// <summary>
        /// Key agreement
        /// </summary>
        /// <param name="privateKey">[in] your private key for key agreement</param>
        /// <param name="peerPublicKey">[in] peer's public key</param>
        /// <returns>shared secret (needs hashing before use)</returns>
        public static byte[] GetSharedSecret(byte[] privateKey, byte[] peerPublicKey)
        {
            var sharedSecret = new byte[32];

            var dx = Unpack(peerPublicKey);


            var t1 = GFN(10);
            var t2 = GFN(10);
            var t3 = GFN(10);
            var t4 = GFN(10);

            /* 0G = point-at-infinity */
            var x = new long[][] { GFN(10, 1), GFN(10) };
            var z = new long[][] { GFN(10), GFN(10) };


            /* 1G = G */
            for (int i = 0; i < x[1].Length; i++)
            {
                x[1][i] = dx[i];
            }
            z[1][0] = 1;

            for (var i = 32; i-- != 0;)
            {
                for (var j = 8; j-- != 0;)
                {
                    /* swap arguments depending on bit */
                    var bit1 = (privateKey[i] & 0xFF) >> j & 1;
                    var bit0 = ~(privateKey[i] & 0xFF) >> j & 1;
                    var ax = x[bit0];
                    var az = z[bit0];
                    var bx = x[bit1];
                    var bz = z[bit1];

                    /* a' = a + b	*/
                    /* b' = 2 b	*/
                    MontyPrepare(t1, t2, ax, az);
                    MontyPrepare(t3, t4, bx, bz);
                    MontyAdd(t1, t2, t3, t4, ax, az, dx);
                    MontyDouble(t1, t2, t3, t4, bx, bz);
                }
            }
            Reciprocal(t1, z[0], false);
            Multiply(dx, x[0], t1);
            Pack(dx, sharedSecret);

            return sharedSecret;
        }

        /********************* radix 2^25.5 GF(2^255-19) math *********************/

        private const int P25 = 33554431; /* (1 << 25) - 1 */
        private const int P26 = 67108863; /* (1 << 26) - 1 */
        /// <summary>
        /// Check if reduced-form input >= 2^255-19
        /// </summary>
        private static bool IsOverflow(long[] x)
        {
            return (
                ((x[0] > P26 - 19)) &
                ((x[1] & x[3] & x[5] & x[7] & x[9]) == P25) &
                ((x[2] & x[4] & x[6] & x[8]) == P26)
                ) || (x[9] > P25);
        }

        private static void Pack(long[] x, byte[] m)
        {
            var ld = (IsOverflow(x) ? 1 : 0) - ((x[9] < 0) ? 1 : 0);
            var ud = ld * -(P25 + 1);
            ld *= 19;
            var t = ld + x[0] + (x[1] << 26);
            m[0] = (byte)t;
            m[1] = (byte)(t >> 8);
            m[2] = (byte)(t >> 16);
            m[3] = (byte)(t >> 24);
            t = (t >> 32) + (x[2] << 19);
            m[4] = (byte)t;
            m[5] = (byte)(t >> 8);
            m[6] = (byte)(t >> 16);
            m[7] = (byte)(t >> 24);
            t = (t >> 32) + (x[3] << 13);
            m[8] = (byte)t;
            m[9] = (byte)(t >> 8);
            m[10] = (byte)(t >> 16);
            m[11] = (byte)(t >> 24);
            t = (t >> 32) + (x[4] << 6);
            m[12] = (byte)t;
            m[13] = (byte)(t >> 8);
            m[14] = (byte)(t >> 16);
            m[15] = (byte)(t >> 24);
            t = (t >> 32) + x[5] + (x[6] << 25);
            m[16] = (byte)t;
            m[17] = (byte)(t >> 8);
            m[18] = (byte)(t >> 16);
            m[19] = (byte)(t >> 24);
            t = (t >> 32) + (x[7] << 19);
            m[20] = (byte)t;
            m[21] = (byte)(t >> 8);
            m[22] = (byte)(t >> 16);
            m[23] = (byte)(t >> 24);
            t = (t >> 32) + (x[8] << 12);
            m[24] = (byte)t;
            m[25] = (byte)(t >> 8);
            m[26] = (byte)(t >> 16);
            m[27] = (byte)(t >> 24);
            t = (t >> 32) + ((x[9] + ud) << 6);
            m[28] = (byte)t;
            m[29] = (byte)(t >> 8);
            m[30] = (byte)(t >> 16);
            m[31] = (byte)(t >> 24);
        }

        private static void Reciprocal(long[] y, long[] x, bool sqrtAssist)
        {
            var t0 = GFN(10);
            var t1 = GFN(10);
            var t2 = GFN(10);
            var t3 = GFN(10);
            var t4 = GFN(10);
            int i;
            /* the chain for x^(2^255-21) is straight from djb's implementation */
            Square(t1, x); /*  2 == 2 * 1	*/
            Square(t2, t1); /*  4 == 2 * 2	*/
            Square(t0, t2); /*  8 == 2 * 4	*/
            Multiply(t2, t0, x); /*  9 == 8 + 1	*/
            Multiply(t0, t2, t1); /* 11 == 9 + 2	*/
            Square(t1, t0); /* 22 == 2 * 11	*/
            Multiply(t3, t1, t2); /* 31 == 22 + 9
					== 2^5   - 2^0	*/
            Square(t1, t3); /* 2^6   - 2^1	*/
            Square(t2, t1); /* 2^7   - 2^2	*/
            Square(t1, t2); /* 2^8   - 2^3	*/
            Square(t2, t1); /* 2^9   - 2^4	*/
            Square(t1, t2); /* 2^10  - 2^5	*/
            Multiply(t2, t1, t3); /* 2^10  - 2^0	*/
            Square(t1, t2); /* 2^11  - 2^1	*/
            Square(t3, t1); /* 2^12  - 2^2	*/
            for (i = 1; i < 5; i++)
            {
                Square(t1, t3);
                Square(t3, t1);
            } /* t3 */ /* 2^20  - 2^10	*/
            Multiply(t1, t3, t2); /* 2^20  - 2^0	*/
            Square(t3, t1); /* 2^21  - 2^1	*/
            Square(t4, t3); /* 2^22  - 2^2	*/
            for (i = 1; i < 10; i++)
            {
                Square(t3, t4);
                Square(t4, t3);
            } /* t4 */ /* 2^40  - 2^20	*/
            Multiply(t3, t4, t1); /* 2^40  - 2^0	*/
            for (i = 0; i < 5; i++)
            {
                Square(t1, t3);
                Square(t3, t1);
            } /* t3 */ /* 2^50  - 2^10	*/
            Multiply(t1, t3, t2); /* 2^50  - 2^0	*/
            Square(t2, t1); /* 2^51  - 2^1	*/
            Square(t3, t2); /* 2^52  - 2^2	*/
            for (i = 1; i < 25; i++)
            {
                Square(t2, t3);
                Square(t3, t2);
            } /* t3 */ /* 2^100 - 2^50 */
            Multiply(t2, t3, t1); /* 2^100 - 2^0	*/
            Square(t3, t2); /* 2^101 - 2^1	*/
            Square(t4, t3); /* 2^102 - 2^2	*/
            for (i = 1; i < 50; i++)
            {
                Square(t3, t4);
                Square(t4, t3);
            } /* t4 */ /* 2^200 - 2^100 */
            Multiply(t3, t4, t2); /* 2^200 - 2^0	*/
            for (i = 0; i < 25; i++)
            {
                Square(t4, t3);
                Square(t3, t4);
            } /* t3 */ /* 2^250 - 2^50	*/
            Multiply(t2, t3, t1); /* 2^250 - 2^0	*/
            Square(t1, t2); /* 2^251 - 2^1	*/
            Square(t2, t1); /* 2^252 - 2^2	*/
            if (sqrtAssist)
            {
                Multiply(y, x, t2); /* 2^252 - 3 */
            }
            else
            {
                Square(t1, t2); /* 2^253 - 2^3	*/
                Square(t2, t1); /* 2^254 - 2^4	*/
                Square(t1, t2); /* 2^255 - 2^5	*/
                Multiply(y, t1, t0); /* 2^255 - 21	*/
            }
        }

        private static void MontyDouble(long[] t1, long[] t2, long[] t3, long[] t4, long[] bx, long[] bz)
        {
            Square(t1, t3);
            Square(t2, t4);
            Multiply(bx, t1, t2);
            Sub(t2, t1, t2);
            MulSmall(bz, t2, 121665);
            Add(t1, t1, bz);
            Multiply(bz, t1, t2);
        }

        private static void MulSmall(long[] xy, long[] x, int y)
        {
            var temp = (x[8] * y);
            xy[8] = (temp & ((1 << 26) - 1));
            temp = (temp >> 26) + (x[9] * y);
            xy[9] = (temp & ((1 << 25) - 1));
            temp = 19 * (temp >> 25) + (x[0] * y);
            xy[0] = (temp & ((1 << 26) - 1));
            temp = (temp >> 26) + (x[1] * y);
            xy[1] = (temp & ((1 << 25) - 1));
            temp = (temp >> 25) + (x[2] * y);
            xy[2] = (temp & ((1 << 26) - 1));
            temp = (temp >> 26) + (x[3] * y);
            xy[3] = (temp & ((1 << 25) - 1));
            temp = (temp >> 25) + (x[4] * y);
            xy[4] = (temp & ((1 << 26) - 1));
            temp = (temp >> 26) + (x[5] * y);
            xy[5] = (temp & ((1 << 25) - 1));
            temp = (temp >> 25) + (x[6] * y);
            xy[6] = (temp & ((1 << 26) - 1));
            temp = (temp >> 26) + (x[7] * y);
            xy[7] = (temp & ((1 << 25) - 1));
            temp = (temp >> 25) + xy[8];
            xy[8] = (temp & ((1 << 26) - 1));
            xy[9] += (temp >> 26);
        }

        private static void MontyAdd(long[] t1, long[] t2, long[] t3, long[] t4, long[] ax, long[] az, long[] dx)
        {
            Multiply(ax, t2, t3);
            Multiply(az, t1, t4);
            Add(t1, ax, az);
            Sub(t2, ax, az);
            Square(ax, t1);
            Square(t1, t2);
            Multiply(az, t1, dx);
        }

        private static void Square(long[] xsqr, long[] x)
        {
            long
                x0 = x[0],
                x1 = x[1],
                x2 = x[2],
                x3 = x[3],
                x4 = x[4],
                x5 = x[5],
                x6 = x[6],
                x7 = x[7],
                x8 = x[8],
                x9 = x[9];

            var t = (x4 * x4) + 2 * ((x0 * x8) + (x2 * x6)) + 38 *
                     (x9 * x9) + 4 * ((x1 * x7) + (x3 * x5));

            xsqr[8] = (t & ((1 << 26) - 1));
            t = (t >> 26) + 2 * ((x0 * x9) + (x1 * x8) + (x2 * x7) +
                               (x3 * x6) + (x4 * x5));
            xsqr[9] = (t & ((1 << 25) - 1));
            t = 19 * (t >> 25) + (x0 * x0) + 38 * ((x2 * x8) +
                                               (x4 * x6) + (x5 * x5)) + 76 * ((x1 * x9)
                                                                            + (x3 * x7));
            xsqr[0] = (t & ((1 << 26) - 1));
            t = (t >> 26) + 2 * (x0 * x1) + 38 * ((x2 * x9) +
                                              (x3 * x8) + (x4 * x7) + (x5 * x6));
            xsqr[1] = (t & ((1 << 25) - 1));
            t = (t >> 25) + 19 * (x6 * x6) + 2 * ((x0 * x2) +
                                              (x1 * x1)) + 38 * (x4 * x8) + 76 *
                ((x3 * x9) + (x5 * x7));
            xsqr[2] = (t & ((1 << 26) - 1));
            t = (t >> 26) + 2 * ((x0 * x3) + (x1 * x2)) + 38 *
                ((x4 * x9) + (x5 * x8) + (x6 * x7));
            xsqr[3] = (t & ((1 << 25) - 1));
            t = (t >> 25) + (x2 * x2) + 2 * (x0 * x4) + 38 *
                ((x6 * x8) + (x7 * x7)) + 4 * (x1 * x3) + 76 *
                (x5 * x9);
            xsqr[4] = (t & ((1 << 26) - 1));
            t = (t >> 26) + 2 * ((x0 * x5) + (x1 * x4) + (x2 * x3))
                + 38 * ((x6 * x9) + (x7 * x8));
            xsqr[5] = (t & ((1 << 25) - 1));
            t = (t >> 25) + 19 * (x8 * x8) + 2 * ((x0 * x6) +
                                              (x2 * x4) + (x3 * x3)) + 4 * (x1 * x5) +
                76 * (x7 * x9);
            xsqr[6] = (t & ((1 << 26) - 1));
            t = (t >> 26) + 2 * ((x0 * x7) + (x1 * x6) + (x2 * x5) +
                               (x3 * x4)) + 38 * (x8 * x9);
            xsqr[7] = (t & ((1 << 25) - 1));
            t = (t >> 25) + xsqr[8];
            xsqr[8] = (t & ((1 << 26) - 1));
            xsqr[9] += (t >> 26);
        }

        private static void Multiply(long[] xy, long[] x, long[] y)
        {
            /* sahn0:
  * Using local variables to avoid class access.
  * This seem to improve performance a bit...
  */
            long
                x0 = x[0],
                x1 = x[1],
                x2 = x[2],
                x3 = x[3],
                x4 = x[4],
                x5 = x[5],
                x6 = x[6],
                x7 = x[7],
                x8 = x[8],
                x9 = x[9];
            long
                y0 = y[0],
                y1 = y[1],
                y2 = y[2],
                y3 = y[3],
                y4 = y[4],
                y5 = y[5],
                y6 = y[6],
                y7 = y[7],
                y8 = y[8],
                y9 = y[9];
            var
                t = (x0 * y8) + (x2 * y6) + (x4 * y4) + (x6 * y2) +
                    (x8 * y0) + 2 * ((x1 * y7) + (x3 * y5) +
                                 (x5 * y3) + (x7 * y1)) + 38 *
                    (x9 * y9);
            xy[8] = (t & ((1 << 26) - 1));
            t = (t >> 26) + (x0 * y9) + (x1 * y8) + (x2 * y7) +
                (x3 * y6) + (x4 * y5) + (x5 * y4) +
                (x6 * y3) + (x7 * y2) + (x8 * y1) +
                (x9 * y0);
            xy[9] = (t & ((1 << 25) - 1));
            t = (x0 * y0) + 19 * ((t >> 25) + (x2 * y8) + (x4 * y6)
                                + (x6 * y4) + (x8 * y2)) + 38 *
                ((x1 * y9) + (x3 * y7) + (x5 * y5) +
                 (x7 * y3) + (x9 * y1));
            xy[0] = (t & ((1 << 26) - 1));
            t = (t >> 26) + (x0 * y1) + (x1 * y0) + 19 * ((x2 * y9)
                                                        + (x3 * y8) + (x4 * y7) + (x5 * y6) +
                                                        (x6 * y5) + (x7 * y4) + (x8 * y3) +
                                                        (x9 * y2));
            xy[1] = (t & ((1 << 25) - 1));
            t = (t >> 25) + (x0 * y2) + (x2 * y0) + 19 * ((x4 * y8)
                                                        + (x6 * y6) + (x8 * y4)) + 2 * (x1 * y1)
                + 38 * ((x3 * y9) + (x5 * y7) +
                      (x7 * y5) + (x9 * y3));
            xy[2] = (t & ((1 << 26) - 1));

            t = (t >> 26) + (x0 * y3) + (x1 * y2) + (x2 * y1) +
                (x3 * y0) + 19 * ((x4 * y9) + (x5 * y8) +
                                (x6 * y7) + (x7 * y6) +
                                (x8 * y5) + (x9 * y4));

            xy[3] = (t & ((1 << 25) - 1));
            t = (t >> 25) + (x0 * y4) + (x2 * y2) + (x4 * y0) + 19 *
                ((x6 * y8) + (x8 * y6)) + 2 * ((x1 * y3) +
                                             (x3 * y1)) + 38 *
                ((x5 * y9) + (x7 * y7) + (x9 * y5));
            xy[4] = (t & ((1 << 26) - 1));
            t = (t >> 26) + (x0 * y5) + (x1 * y4) + (x2 * y3) +
                (x3 * y2) + (x4 * y1) + (x5 * y0) + 19 *
                ((x6 * y9) + (x7 * y8) + (x8 * y7) +
                 (x9 * y6));
            xy[5] = (t & ((1 << 25) - 1));
            t = (t >> 25) + (x0 * y6) + (x2 * y4) + (x4 * y2) +
                (x6 * y0) + 19 * (x8 * y8) + 2 * ((x1 * y5) +
                                              (x3 * y3) + (x5 * y1)) + 38 *
                ((x7 * y9) + (x9 * y7));
            xy[6] = (t & ((1 << 26) - 1));
            t = (t >> 26) + (x0 * y7) + (x1 * y6) + (x2 * y5) +
                (x3 * y4) + (x4 * y3) + (x5 * y2) +
                (x6 * y1) + (x7 * y0) + 19 * ((x8 * y9) +
                                            (x9 * y8));
            xy[7] = (t & ((1 << 25) - 1));
            t = (t >> 25) + xy[8];
            xy[8] = (t & ((1 << 26) - 1));
            xy[9] += (t >> 26);
        }

        private static void MontyPrepare(long[] t1, long[] t2, long[] ax, long[] az)
        {
            Add(t1, ax, az);
            Sub(t2, ax, az);

        }

        private static void Add(long[] t1, long[] ax, long[] az)
        {
            for (var i = 0; i < t1.Length; i++)
                t1[i] = ax[i] + az[i];
        }
        private static void Sub(long[] t1, long[] ax, long[] az)
        {
            for (var i = 0; i < t1.Length; i++)
                t1[i] = ax[i] - az[i];
        }

        private static long[] Unpack(byte[] m)
        {
            return new long[] {
            ((m[0] & 0xFF)) | ((m[1] & 0xFF)) << 8 |
            (m[2] & 0xFF) << 16 | ((m[3] & 0xFF) & 3) << 24,
            ((m[3] & 0xFF) & ~3) >> 2 | (m[4] & 0xFF) << 6 |
            (m[5] & 0xFF) << 14 | ((m[6] & 0xFF) & 7) << 22,
            ((m[6] & 0xFF) & ~7) >> 3 | (m[7] & 0xFF) << 5 |
            (m[8] & 0xFF) << 13 | ((m[9] & 0xFF) & 31) << 21,
            ((m[9] & 0xFF) & ~31) >> 5 | (m[10] & 0xFF) << 3 |
            (m[11] & 0xFF) << 11 | ((m[12] & 0xFF) & 63) << 19,
            ((m[12] & 0xFF) & ~63) >> 6 | (m[13] & 0xFF) << 2 |
            (m[14] & 0xFF) << 10 | (m[15] & 0xFF) << 18,
            (m[16] & 0xFF) | (m[17] & 0xFF) << 8 |
            (m[18] & 0xFF) << 16 | ((m[19] & 0xFF) & 1) << 24,
            ((m[19] & 0xFF) & ~1) >> 1 | (m[20] & 0xFF) << 7 |
            (m[21] & 0xFF) << 15 | ((m[22] & 0xFF) & 7) << 23,
            ((m[22] & 0xFF) & ~7) >> 3 | (m[23] & 0xFF) << 5 |
            (m[24] & 0xFF) << 13 | ((m[25] & 0xFF) & 15) << 21,
            ((m[25] & 0xFF) & ~15) >> 4 | (m[26] & 0xFF) << 4 |
            (m[27] & 0xFF) << 12 | ((m[28] & 0xFF) & 63) << 20,
            ((m[28] & 0xFF) & ~63) >> 6 | (m[29] & 0xFF) << 2 |
                   (m[30] & 0xFF) << 10 | (m[31] & 0xFF) << 18,
        };
        }

        #endregion
    }
}
