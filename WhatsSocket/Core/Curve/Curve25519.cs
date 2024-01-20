using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using WebSocketSharp;

namespace WhatsSocket.Core.Curve
{

    public class Curve25519
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
        private static UInt64[] K = new UInt64[80]
{
          0x428a2f98d728ae22, 0x7137449123ef65cd, 0xb5c0fbcfec4d3b2f, 0xe9b5dba58189dbbc,
          0x3956c25bf348b538, 0x59f111f1b605d019, 0x923f82a4af194f9b, 0xab1c5ed5da6d8118,
          0xd807aa98a3030242, 0x12835b0145706fbe, 0x243185be4ee4b28c, 0x550c7dc3d5ffb4e2,
          0x72be5d74f27b896f, 0x80deb1fe3b1696b1, 0x9bdc06a725c71235, 0xc19bf174cf692694,
          0xe49b69c19ef14ad2, 0xefbe4786384f25e3, 0x0fc19dc68b8cd5b5, 0x240ca1cc77ac9c65,
          0x2de92c6f592b0275, 0x4a7484aa6ea6e483, 0x5cb0a9dcbd41fbd4, 0x76f988da831153b5,
          0x983e5152ee66dfab, 0xa831c66d2db43210, 0xb00327c898fb213f, 0xbf597fc7beef0ee4,
          0xc6e00bf33da88fc2, 0xd5a79147930aa725, 0x06ca6351e003826f, 0x142929670a0e6e70,
          0x27b70a8546d22ffc, 0x2e1b21385c26c926, 0x4d2c6dfc5ac42aed, 0x53380d139d95b3df,
          0x650a73548baf63de, 0x766a0abb3c77b2a8, 0x81c2c92e47edaee6, 0x92722c851482353b,
          0xa2bfe8a14cf10364, 0xa81a664bbc423001, 0xc24b8b70d0f89791, 0xc76c51a30654be30,
          0xd192e819d6ef5218, 0xd69906245565a910, 0xf40e35855771202a, 0x106aa07032bbd1b8,
          0x19a4c116b8d2d0c8, 0x1e376c085141ab53, 0x2748774cdf8eeb99, 0x34b0bcb5e19b48a8,
          0x391c0cb3c5c95a63, 0x4ed8aa4ae3418acb, 0x5b9cca4f7763e373, 0x682e6ff3d6b2b8a3,
          0x748f82ee5defb2fc, 0x78a5636f43172f60, 0x84c87814a1f0ab72, 0x8cc702081a6439ec,
          0x90befffa23631e28, 0xa4506cebde82bde9, 0xbef9a3f7b2c67915, 0xc67178f2e372532b,
          0xca273eceea26619c, 0xd186b8c721c0c207, 0xeada7dd6cde0eb1e, 0xf57d4f7fee6ed178,
          0x06f067aa72176fba, 0x0a637dc5a2c898a6, 0x113f9804bef90dae, 0x1b710b35131c471b,
          0x28db77f523047d84, 0x32caab7b40c72493, 0x3c9ebe0a15c9bebc, 0x431d67c49c100d4c,
          0x4cc5d4becb3e42b6, 0x597f299cfc657e2a, 0x5fcb6fab3ad6faec, 0x6c44198c4a475817
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




        internal static bool Verify(byte[] publicKey, byte[] message, byte[] signature)
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
            var sm = new byte[64 + message.Length];
            var m = new byte[64 + message.Length];
            for (var i = 0; i < 64; i++)
                sm[i] = signature[i];
            for (var i = 0; i < message.Length; i++)
                sm[i + 64] = message[i];


            return curve25519_sign_open(m, sm, sm.Length, publicKey) >= 0;
        }

        private static int curve25519_sign_open(byte[] m, byte[] sm, int n, byte[] pk)
        {
            // Convert Curve25519 public key into Ed25519 public key.
            var edpk = convertPublicKey(pk);
            // Restore sign bit from signature.
            edpk[31] |= (byte)(sm[63] & 128);
            // Remove sign bit from signature.
            sm[63] &= 127;
            // Verify signed message.

            return crypto_sign_open(m, sm, n, edpk);
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



        private static int crypto_sign_open(byte[] m, byte[] sm, int n, byte[] pk)
        {
            var mlen = -1;
            Byte[] t = new Byte[32];
            Byte[] h = new Byte[64];
            var p = new long[][] { GF(), GF(), GF(), GF() };
            var q = new long[][] { GF(), GF(), GF(), GF() };
            if (n < 64)
                return -1;
            if (Unpackneg(q, pk) != 0)
                return -1;
            for (var i = 0; i < n; i++)
                m[i] = sm[i];
            for (var i = 0; i < 32; i++)
                m[i + 32] = pk[i];
            CryptoHash(h, m, n);
            Reduce(h);
            Scalarmult(p, q, h);
            var subArray = sm.Take(32).ToArray();
            Scalarbase(p, subArray);
            Add(p, q);
            Pack(t, p);
            n -= 64;

            if (CryptoVerify32(sm, 0, t, 0) != 0)
            {
                for (var i = 0; i < n; i++)
                    m[i] = 0;
                return -1;
            }

            for (var i = 0; i < n; i++)
                m[i] = sm[i + 64];
            mlen = n;
            return mlen;
        }

        private static int CryptoVerify32(byte[] x, int xi, byte[] y, int yi)
        {
            return Vn(x, xi, y, yi, 32);
        }

        private static int Vn(byte[] x, int xi, byte[] y, int yi, int n)
        {
            int i, d = 0;
            for (i = 0; i < n; i++)
                d |= x[xi + i] ^ y[yi + i];
            return (1 & ((d - 1) >>> 8)) - 1;
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
            return CryptoVerify32(c, 0, d, 0);
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

        public static Int32 CryptoHash(Byte[] hash, Byte[] message, Int32 n)
        {
            Byte[] h = new Byte[64];
            Byte[] x = new Byte[256];
            Int32 b = n;

            for (var i = 0; i < 64; i++)
            {
                h[i] = iv[i];
            }

            CryptoHashBlocks(h, message, n);

            for (var i = 0; i < 64; i++)
            {
                for (var j = 0; j < message.Length; j++)
                {
                    message[j] += (Byte)n;
                }

                n &= 127;

                for (var j = 0; j < message.Length; j++)
                {
                    message[j] -= (Byte)n;
                }
            }

            for (var i = 0; i < 256; i++)
            {
                x[i] = 0;
            }

            for (var i = 0; i < n; i++)
            {
                x[i] = message[i];
            }

            x[n] = 128;

            n = ((n < 112) ? 256 - 128 * 1 : 256 - 128 * 0);
            x[n - 9] = (Byte)(b >> 61);

            Ts64(x, (UInt64)b << 3, n - 8);

            CryptoHashBlocks(h, x, n);

            for (var i = 0; i < 64; i++)
            {
                hash[i] = h[i];
            }

            return 0;
        }


        private static void Ts64(Byte[] x, UInt64 u, Int32 offset = 0)
        {
            for (var i = 7; i >= 0; --i)
            {
                x[i + offset] = (Byte)u; u >>= 8;
            }
        }

        private static UInt64 Dl64(Byte[] x, Int64 offset)
        {
            UInt64 u = 0;
            for (var i = 0; i < 8; ++i) u = (u << 8) | x[i + offset];
            return u;
        }

        private static UInt64 R(UInt64 x, int c) { return (x >> c) | (x << (64 - c)); }
        private static UInt64 Ch(UInt64 x, UInt64 y, UInt64 z) { return (x & y) ^ (~x & z); }

        private static UInt64 Maj(UInt64 x, UInt64 y, UInt64 z) { return (x & y) ^ (x & z) ^ (y & z); }
        private static UInt64 Sigma0(UInt64 x) { return R(x, 28) ^ R(x, 34) ^ R(x, 39); }

        private static UInt64 Sigma1(UInt64 x) { return R(x, 14) ^ R(x, 18) ^ R(x, 41); }

        private static UInt64 sigma0(UInt64 x) { return R(x, 1) ^ R(x, 8) ^ (x >> 7); }

        private static UInt64 sigma1(UInt64 x) { return R(x, 19) ^ R(x, 61) ^ (x >> 6); }

        private static Int32 CryptoHashBlocks(Byte[] x, Byte[] m, Int32 n)
        {
            UInt64[] z = new UInt64[8];
            UInt64[] b = new UInt64[8];
            UInt64[] a = new UInt64[8];
            UInt64[] w = new UInt64[16];
            UInt64 t = 0;

            for (var i = 0; i < 8; i++)
            {
                z[i] = a[i] = Dl64(x, 8 * i);
            }

            while (n >= 128)
            {
                for (var i = 0; i < 16; i++)
                {
                    w[i] = Dl64(m, 8 * i);
                }

                for (var i = 0; i < 80; i++)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        b[j] = a[j];
                    }

                    t = a[7] + Sigma1(a[4]) + Ch(a[4], a[5], a[6]) + K[i] + w[i % 16];
                    b[7] = t + Sigma0(a[0]) + Maj(a[0], a[1], a[2]);
                    b[3] += t;
                    for (var j = 0; j < 8; j++)
                    {
                        a[(j + 1) % 8] = b[j];
                    }

                    if (i % 16 == 15)
                        for (var j = 0; j < 16; j++)
                        {
                            w[j] += w[(j + 9) % 16] + sigma0(w[(j + 1) % 16]) + sigma1(w[(j + 14) % 16]);
                        }
                }

                for (var i = 0; i < 8; i++)
                {
                    a[i] += z[i]; z[i] = a[i];
                }

                for (var i = 0; i < m.Length; i++)
                {
                    m[i] += 128;
                }

                n -= 128;
            }

            for (var i = 0; i < 8; i++)
            {
                Ts64(x, z[i], 8 * i);
            }

            return n;
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

        #endregion
    }
}
