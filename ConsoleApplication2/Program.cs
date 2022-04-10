using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using i4o;
using i4o.old;
using System.IO;
using System.Diagnostics;

namespace ConsoleApplication2
{
    class Program
    {
        static List<Boo> pool = new List<Boo>();

        class Boo

        {

            public int No { get; set; }

            public int SubNo { get; set; }

            public string Code;

        }

        static void Main(string[] args)
        {
            //string dir = @"C:\Lancom\";
            //var fileInfosFromDir = (from f in System.IO.Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
            //                        select new FileInfo(f)).ToList();

            //var spec = new i4o.old.IndexSpecification<FileInfo>()
            //   .Add(i => i.Extension)
            //   .Add(i => i.IsReadOnly);

            //var indexedFileInfosFromDir = fileInfosFromDir.ToIndexableCollection(spec);

            Stopwatch mySW = new Stopwatch();

            //mySW.Reset();
            //mySW.Start();
            //var IndexableCollection_Query_ToIenum=indexedFileInfosFromDir.Where(o => o.Extension == @".cs");
            //mySW.Stop();
            //Console.WriteLine("IndexableCollection_Query_Ienum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //mySW.Reset();
            //mySW.Start();
            //var IndexableCollection_Query_ToList = indexedFileInfosFromDir.Where(o => o.Extension == @".cs").ToList();
            //mySW.Stop();
            //Console.WriteLine("IndexableCollection_Query_ToList Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //mySW.Reset();
            //mySW.Start();
            //var OnlyList_Query_ToIenum = fileInfosFromDir.Where(o => o.Extension == @".cs");
            //mySW.Stop();
            //Console.WriteLine("OnlyList_Query_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //mySW.Reset();
            //mySW.Start();
            //var OnlyList_Query_ToList = fileInfosFromDir.Where(o => o.Extension == @".cs").ToList();
            //mySW.Stop();
            //Console.WriteLine("OnlyList_Query_ToList Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            int MAX_NO = 1000;

            //使用相口同亂數種子確保每次執行之測試資料相同

            Random rnd = new Random(9527); //交給你了，9527

            //建立大量物件集合

            for (int i = 0; i < MAX_NO; i++)

            {

                for (int j = 0; j < rnd.Next(1000, 1000); j++)

                {

                    pool.Add(new Boo()

                    {

                        No = i,

                        SubNo = j,

                        Code = "C" + rnd.Next(1000).ToString("000")

                    });

                }

            }

            int TIMES = 500;

            List<Boo> toFill = new List<Boo>();

            for (int i = 0; i < TIMES; i++)

            {

                Boo sample = pool[rnd.Next(pool.Count)];

                toFill.Add(new Boo()

                {

                    No = sample.No,
                    SubNo = sample.SubNo

                });

            }

            var spac2 = new i4o.old.IndexSpecification<Boo>()
               .Add(i => i.No);

            var poolindexed = pool.ToIndexableCollection<Boo>(spac2);

            Console.WriteLine("C# \r\n");

            //一般List查詢 不轉成List()
            mySW.Reset();
            mySW.Start();
            var OnlyPoolList_Query_ToIenum = pool.Where(o => o.No == 0);
            mySW.Stop();
            Console.WriteLine("OnlyPoolList_Query_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //一般List查詢 轉成List()
            mySW.Reset();
            mySW.Start();
            var OnlyPoolList_Query_ToList = pool.Where(o => o.No == 0).ToList();
            mySW.Stop();
            Console.WriteLine("OnlyPoolList_Query_ToList Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            Console.WriteLine("\r\n");

            //mySW.Reset();
            //mySW.Start();
            //var poolIndexableCollection_Query_ToIenum = poolindexed.Where(o => o.No == 0);
            //mySW.Stop();
            //Console.WriteLine("poolIndexableCollection_Query_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //mySW.Reset();
            //mySW.Start();
            //var poolIndexableCollection_Query_Tolist = poolindexed.Where(o => o.No == 0).ToList();
            //mySW.Stop();
            //Console.WriteLine("poolIndexableCollection_Query_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //Console.WriteLine("\r\n");

            //mySW.Reset();
            //mySW.Start();
            //var pool_Plinq_Quer_ToIenum = pool.AsParallel().Where(o => o.No == 0);
            //mySW.Stop();
            //Console.WriteLine("pool_Plinq_Quer_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //mySW.Reset();
            //mySW.Start();
            //var pool_Plinq_Quer_ToList = pool.AsParallel().Where(o => o.No == 0).ToList();
            //mySW.Stop();
            //Console.WriteLine("pool_Plinq_Quer_ToList Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            //Console.WriteLine("\r\n");

            var spac3 = new i4o.IndexSpecification<Boo>()
               .Add(i => i.No);

            IndexSet<Boo> poolSet = new IndexSet<Boo>(pool, spac3);

            mySW.Reset();
            mySW.Start();
            var poolSet_Query_ToIenum = poolSet.Where(o => o.No == 0);
            mySW.Stop();
            Console.WriteLine("poolSet_Query_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            mySW.Reset();
            mySW.Start();
            var poolSet_Query_ToList = poolSet.Where(o => o.No == 0).ToList();
            mySW.Stop();
            Console.WriteLine("poolSet_Query_ToList Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            Console.WriteLine("\r\n");

            mySW.Reset();
            mySW.Start();
            var pool_PlinqWithExecutionMode_Quer_ToIenum = pool.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).Where(o => o.No == 0);
            mySW.Stop();
            Console.WriteLine("pool_PlinqWithExecutionMode_Quer_ToIenum Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            mySW.Reset();
            mySW.Start();
            var pool_PlinqWithExecutionMode_Quer_ToList = pool.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).Where(o => o.No == 0).ToList();
            mySW.Stop();
            Console.WriteLine("pool_PlinqWithExecutionMode_Quer_ToList Query time: {0:0.00} msec", mySW.Elapsed.TotalMilliseconds);

            

            

            Console.ReadLine();
        }
    }
}
