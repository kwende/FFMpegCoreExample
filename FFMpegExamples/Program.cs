using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extend;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace FFMpegExamples
{
    class Program
    {
        static DateTimeOffset _firstFrameProcessedAt = DateTimeOffset.MinValue; 

        static IVideoFrame GenerateRandomImage(int frameNumber, int width, int height)
        {
            // this is in place because starting up the process might have an overhead. 
            if(_firstFrameProcessedAt == DateTimeOffset.MinValue)
            {
                _firstFrameProcessedAt = DateTimeOffset.Now; 
            }

            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            BitmapData bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            try
            {
                byte[] rgb = new byte[bmd.Stride * bmd.Height]; 
                for(int y=0;y<height;y++)
                {
                    for(int x=0;x<width;x++)
                    {
                        int index = (y * width + x) * 3;

                        rgb[index] =   (byte)(frameNumber * y + x);
                        rgb[index+1] = (byte)(frameNumber * y + x);
                        rgb[index+2] = (byte)(frameNumber * y + x);
                    }
                }

                Marshal.Copy(rgb, 0, bmd.Scan0, rgb.Length); 
            }
            finally
            {
                bmp.UnlockBits(bmd); 
            }

            return new BitmapVideoFrameWrapper(bmp); 
        }


        static IEnumerable<IVideoFrame> ReadFakeFile(int numberOfFramesToRead)
        {
            for(int c=0;c<numberOfFramesToRead;c++)
            {
                yield return GenerateRandomImage(c, 256, 256); 
            }
        }

        static void Main(string[] args)
        {
            const string OutputPath = "output.mp4";
            const int NumberOfFrames = 1000;
            const int FrameRate = 30;

            Console.WriteLine($"Video should be {NumberOfFrames / FrameRate} seconds long."); 

            if(File.Exists(OutputPath))
            {
                File.Delete(OutputPath); 
            }

            RawVideoPipeSource videoFramesSource = new RawVideoPipeSource(ReadFakeFile(NumberOfFrames)) 
            {
                FrameRate = 30 //set source frame rate
            };

            FFMpegArguments
                .FromPipeInput(videoFramesSource)
                .OutputToFile(OutputPath, false, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithFramerate(30)) // NOTE: you can specify additional encoding parameters here. 
                .ProcessAsynchronously().Wait();

            TimeSpan ts = DateTimeOffset.Now - _firstFrameProcessedAt; 

            Console.WriteLine($"Took {ts}. {NumberOfFrames / ts.TotalSeconds} fps"); 
        }
    }
}
