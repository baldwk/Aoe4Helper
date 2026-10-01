using AduSkin.Demo.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace AduSkin.Demo.Data
{
   internal static class CarouselSamples
   {
      public static ObservableCollection<Carousel> Create() => new(Enumerable.Range(0, 5).Select(_ => new Carousel
      {
         imgpath = "../Resources/aduskin.png",
         name = "AduSkin",
         info = "追求极致，永臻完美"
      }));
   }
}
