﻿using System;
using System.IO;
using System.Text;

using AirBreather.Csv;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using CsvHelper;
using CsvHelper.Configuration;

namespace AirBreather.Bench
{
    [ClrJob]
    [CoreJob]
    [CoreRtJob]
    [GcServer(true)]
    [MemoryDiagnoser]
    public class Program
    {
        private byte[] data;

        [GlobalSetup]
        public void Setup()
        {
            this.data = File.ReadAllBytes(@"N:\media\datasets\csv-from-nz\Building consents by institutional control (Monthly).csv");
        }

        [Benchmark(Baseline = true)]
        public long CountRowsUsingMine()
        {
            var visitor = new RowCountingVisitor();
            var tokenizer = new CsvTokenizer();
            tokenizer.ProcessNextChunk(this.data, visitor);
            tokenizer.ProcessEndOfStream(visitor);
            return visitor.RowCount;
        }

        [Benchmark]
        public long CountRowsUsingCsvHelper()
        {
            using (var ms = new MemoryStream(this.data, false))
            using (var tr = new StreamReader(ms, new UTF8Encoding(false, false), false))
            using (var rd = new CsvReader(tr, new Configuration { BadDataFound = null }))
            {
                long cnt = 0;
                while (rd.Read())
                {
                    ++cnt;
                }

                return cnt;
            }
        }

        static void Main()
        {
            var prog = new Program();
            prog.Setup();
            Console.WriteLine(prog.CountRowsUsingMine());
            Console.WriteLine(prog.CountRowsUsingCsvHelper());

            BenchmarkRunner.Run<Program>();
        }

        private sealed class RowCountingVisitor : CsvReaderVisitorBase
        {
            public long RowCount { get; private set; }

            public override void VisitEndOfRecord() => ++this.RowCount;
            public override void VisitEndOfField(ReadOnlySpan<byte> chunk) { }
            public override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk) { }
        }
    }
}
