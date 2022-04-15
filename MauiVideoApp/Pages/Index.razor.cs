using Android.Content;
using Laerdal.FFmpeg;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility;
using Plugin.CurrentActivity;
using System.Diagnostics;

namespace MauiVideoApp.Pages
{
    public partial class Index
    {
        private string outputFile;
        IBrowserFile file;
        private async void LoadFile(InputFileChangeEventArgs e)
        {
            file = e.File;
            videoSelected = true;
            await JS.InvokeVoidAsync("SetVideoSrc");
            StateHasChanged();

        }
        protected override async Task OnInitializedAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            await JS.InvokeVoidAsync("Load");


        }

        private async Task OpenFolder()
        {
            var result=Launcher.OpenAsync
                (new OpenFileRequest()
                {
                    File = new ReadOnlyFile("/storage/emulated/0/Download/yyy_flowers.mp4"),
                }
            ).Result;
                       // var activity = CrossCurrentActivity.Current.Activity;


           // Intent intent = new Intent(Intent.ActionGetContent);
           //// intent.SetType(mime);
           // intent.SetDataAndType(Android.Net.Uri.Parse( Android.OS.Environment.DirectoryDownloads), "*/*");
           // // intent.AddCategory(Intent.CategoryOpenable);

           // activity.OpenFileInput("yyy_flowers.mp4");

            
            //StartActivityForResult(intent, 1);
            //var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            //if (status == PermissionStatus.Granted && file!=null)
            //{

            //        //savetempfile

            //        var directory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);

            //    //  Byte[] bytes = File.ReadAllBytes(pickedFile.FullPath);

            //    //await JS.InvokeVoidAsync("save","oz.mp4",bytes);

            //    //string fullPath = "";
            //    //FFmpegConfig.IgnoreSignal(24);
            //    //var fffInfo = Laerdal.FFmpeg.Android.FFprobe.GetMediaInformation(fullPath);

            //    //duration = double.Parse(fffInfo.Duration.Replace(".", "")) / 100000;

            //  //  await JS.InvokeVoidAsync("SliderLoad", duration * 6);



            //}
        }
        double duration = 0;
        Timer timer;
        private async Task<bool> StartFF()
        {
            var directory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);

            var path = directory + "/xxx_" + file.Name;
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            await using FileStream fs = new(path, FileMode.Create);
            await file.OpenReadStream(512000000).CopyToAsync(fs);
            var extension = new FileInfo(path).Extension;
            outputFile = directory + "/o_" +  file.Name.Replace(extension, convertionFormat);



            if (System.IO.File.Exists(outputFile))
            {
                System.IO.File.Delete(outputFile);
            }

            FFmpegConfig.IgnoreSignal(24);
            int r = 0;


            var task = Task.Run(() =>
            {

                var command = GetCommand(path, outputFile);
                Debug.WriteLine(command);
                r = Laerdal.FFmpeg.Android.FFmpeg
                    .Execute(command);
                timer.Dispose();

            });

            timer = new System.Threading.Timer(async _ =>
            {

                var x = FFmpegConfig.LastReceivedFFmpegStatistics;
                if (r != 0)
                {
                    timer.Dispose();

                }

                if (x != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        int renderedPercantage = (int)((double)x.NativeStatistics.Time / duration);
                        Debug.WriteLine("bweInterop.UpdatePercentage:"+ renderedPercantage);
                        JS.InvokeVoidAsync("bweInterop.UpdatePercentage", renderedPercantage);

                    });

                }

            }, null, 0, 500);

            task.Wait();
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            return true;

        }

        private string GetCommand(string path,string output)
        {
            switch (convertionFormat)
            {
                case ".gif": return "-i " + path + " -t " + from + " -ss " + (to - from) + " -vf \"fps="+ (string.IsNullOrEmpty(fps) ? "5" : fps) + ",scale="+ (string.IsNullOrEmpty(scale) ? "320" : fps) + ":-1:flags = lanczos,split[s0][s1]; [s0] palettegen[p]; [s1][p] paletteuse\" -loop 0 -y "+ output;
                case ".mp4": return "-i " + path + " -t " + from + " -ss " + (to - from) + " -framerate "+ (string.IsNullOrEmpty(fps)?"30":fps)+ " -pix_fmt yuv420p -y " + output;
                case ".mov": return "-i " + path + " -t " + from + " -ss " + (to - from) + " -framerate " + (string.IsNullOrEmpty(fps) ? "30" : fps) + " -pix_fmt yuv420p -y " + output;
                default: return "";
                  
            }
        }

        private async Task ConvertVideo()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                convertStarted = true;
                StateHasChanged();

            });

            var opResult = await StartFF();
            if (opResult)
            {
                
             
                if (convertionFormat==".gif")
                {
                    sourceGif = "data:image/gif;base64," + Convert.ToBase64String(File.ReadAllBytes(outputFile));

                }
                else
                {
                    ShowFile(outputFile);
                    sourceGif = null;
                    videoSelected = false;
                    
                }


                convertCompleted = true;
               await JS.InvokeVoidAsync("CompleteVideo");
                StateHasChanged();
            }

        }
        private async void ShowFile(string path)
        {
            var result = Launcher.OpenAsync
                (new OpenFileRequest()
                {
                    File = new ReadOnlyFile(path),
                }
            ).Result;
            //var mime = SetMimeType(convertionFormat);
            //var activity = CrossCurrentActivity.Current.Activity;
            //Intent intent = new Intent(Intent.ActionGetContent);
            //// intent.SetType(mime);
            //intent.SetDataAndType(Android.Net.Uri.Parse(Android.OS.Environment.DirectoryDownloads),mime);
            //// intent.AddCategory(Intent.CategoryOpenable);

            //activity.StartActivityForResult(intent, 1);



            //Intent intent = new Intent(Intent.ActionGetContent);
            //intent.SetType(mime);
            //intent.AddCategory(Intent.CategoryOpenable);

            //activity.StartActivityForResult(intent, 1);
        }

        private string SetMimeType(string convertionFormat)
        {
            switch (convertionFormat)
            {
                case ".gif": return "image/gif";
                case ".mp4": return "video/mp4";
                case ".mov": return "video/quicktime";
                case ".avi": return "video/avi";

                default:
                    return "";
                    break;
            }
        }
    }

}
