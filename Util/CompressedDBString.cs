using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZstdNet;

namespace DocumentDatabase.Util
{

    public class CompressedDBString
    {
        public string Data { get; set; }
    }

    public class CompressedDBStringTypeConfiguration : IEntityTypeConfiguration<CompressedDBString>
    {
        public void Configure(EntityTypeBuilder<CompressedDBString> builder)
        {
            builder.Property(e => e.Data)
                .HasConversion(
                    v => Zip(v),
                    v => Unzip(v)).UsePropertyAccessMode(PropertyAccessMode.Property);

            builder.HasNoKey();
        }

        public void Configure(PropertyBuilder<CompressedDBString> builder)
        {
            builder
                .HasConversion(
                    v => Zip(v.Data),
                    v => new CompressedDBString { Data = Unzip(v) }).UsePropertyAccessMode(PropertyAccessMode.Property);

        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new CompressionStream(mso, CompressionOptions.MaxCompressionLevel))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new DecompressionStream(msi, CompressionOptions.MaxCompressionLevel))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                var result = Encoding.UTF8.GetString(mso.ToArray());
                return result;
            }
        }
    }
}
