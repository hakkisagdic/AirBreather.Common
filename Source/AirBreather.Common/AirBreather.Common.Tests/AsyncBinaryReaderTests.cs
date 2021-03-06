﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using AirBreather.IO;

using Xunit;

namespace AirBreather.Tests
{
    public sealed class AsyncBinaryReaderTests
    {
        [Fact]
        public async Task TestAsyncBinaryReader()
        {
            byte customByte = 39;
            bool customBool = Unsafe.As<byte, bool>(ref customByte);

            object[] expectedResults =
            {
                true,
                false,
                customBool,
                (byte)42,
                (sbyte)-28,
                (short)-279,
                (ushort)64221,
                (int)-288888,
                (uint)3310229011,
                (float)3811.55f,
                (long)-19195205991011,
                (ulong)11223372036854775807,
                (double)Math.PI,
                (decimal)295222.2811m
            };

            using (var ms = new MemoryStream())
            {
                using (var wr = new BinaryWriter(ms, Encoding.Default, leaveOpen: true))
                {
                    foreach (dynamic obj in expectedResults)
                    {
                        wr.Write(obj);
                    }
                }

                ms.Position = 0;

                using (var rd = new AsyncBinaryReader(ms, Encoding.Default, leaveOpen: true))
                {
                    foreach (var obj in expectedResults)
                    {
                        switch (obj)
                        {
                            case bool b8:
                                ////Assert.Equal(b8, await rd.ReadBooleanAsync());
                                if (b8)
                                {
                                    Assert.True(await rd.ReadBooleanAsync());
                                }
                                else
                                {
                                    Assert.False(await rd.ReadBooleanAsync());
                                }

                                break;

                            case byte u8:
                                Assert.Equal(u8, await rd.ReadByteAsync());
                                break;

                            case sbyte s8:
                                Assert.Equal(s8, await rd.ReadSByteAsync());
                                break;

                            case short s16:
                                Assert.Equal(s16, await rd.ReadInt16Async());
                                break;

                            case ushort u16:
                                Assert.Equal(u16, await rd.ReadUInt16Async());
                                break;

                            case int s32:
                                Assert.Equal(s32, await rd.ReadInt32Async());
                                break;

                            case uint u32:
                                Assert.Equal(u32, await rd.ReadUInt32Async());
                                break;

                            case long s64:
                                Assert.Equal(s64, await rd.ReadInt64Async());
                                break;

                            case ulong u64:
                                Assert.Equal(u64, await rd.ReadUInt64Async());
                                break;

                            case float f32:
                                Assert.Equal(f32, await rd.ReadSingleAsync());
                                break;

                            case double f64:
                                Assert.Equal(f64, await rd.ReadDoubleAsync());
                                break;

                            case decimal d128:
                                Assert.Equal(d128, await rd.ReadDecimalAsync());
                                break;
                        }
                    }
                }
            }
        }
    }
}
