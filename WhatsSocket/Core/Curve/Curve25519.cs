using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security.Certificates;
using Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;

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


        #endregion
    }
}
