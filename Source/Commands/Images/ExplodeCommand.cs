using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using WinBot.Util;
using WinBot.Commands.Attributes;

using ImageMagick;

namespace WinBot.Commands.Images
{
    public class ExplodeCommand : BaseCommandModule
    {
        [Command("explode")]
        [Description("explode an image")]
        [Usage("[image] [-scale]")]
        [Category(Category.Images)]
        public async Task Explode(CommandContext Context, [RemainingText]string input)
        {
            // Handle arguments
            ImageArgs args = ImageCommandParser.ParseArgs(Context, input);
            int seed = new System.Random().Next(1000, 99999);
            args.scale+=2;

            // Download the image
            string tempImgFile = TempManager.GetTempFile(seed+"-explodeDL."+args.extension, true);
            File.WriteAllBytes(tempImgFile, await new HttpClient().GetByteArrayAsync(args.url));

            var msg = await Context.ReplyAsync("Processing...\nThis may take a while depending on the image size");

            // E x p l o d e
            MagickImage img = null;
            MagickImageCollection gif = null;
            if(args.extension.ToLower() != "gif") {
                img = new MagickImage(tempImgFile);
                DoExplode(img, args);
            }
            else {
                gif = new MagickImageCollection(tempImgFile);
                foreach(var frame in gif) {
                    DoExplode((MagickImage)frame, args);
                }
            }
            TempManager.RemoveTempFile(seed+"-explodeDL."+args.extension);
            if(args.extension.ToLower() != "gif")
                args.extension = img.Format.ToString().ToLower();

            // Save the image
            MemoryStream imgStream = new MemoryStream();
            if(args.extension.ToLower() != "gif")
                img.Write(imgStream);
            else
                gif.Write(imgStream);
            imgStream.Position = 0;

            // Send the image
            await msg.ModifyAsync("Uploading...\nThis may take a while depending on the image size");
            await Context.Channel.SendFileAsync(imgStream, "explode."+args.extension);
            await msg.DeleteAsync();
        }

        public static void DoExplode(MagickImage img, ImageArgs args)
        {
            img.Scale(img.Width/2, img.Height/2);
            img.Implode(args.scale*-.3f, PixelInterpolateMethod.Undefined);
            img.Scale(img.Width*2, img.Height*2);
        }
    }
}