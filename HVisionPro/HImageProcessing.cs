using Cognex.VisionPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HVisionPro
{
    public class HImageProcessing
    {
        public static CogImage8Grey Get8GrayImage(ICogImage image)
        {
            CogImage8Grey greyImage;
            if(image.GetType() != typeof(CogImage8Grey))
            {
                greyImage = CogImageConvert.GetIntensityImage(image, 0, 0, image.Width, image.Height);
            }
            else
            {
                greyImage = (CogImage8Grey)image;
            }

            return greyImage;
        }
    }
}
