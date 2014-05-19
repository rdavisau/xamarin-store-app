using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

namespace XamarinStore
{
	public enum ProductType
	{
		MensCSharpShirt,
		WomensCSharpShirt,
		PlushMonkey,
	}

	// so we can use GetType() to identify surprise products and not alter the json output for the webservice
	public class SurpriseProduct : Product, ICloneable
	{
		public static readonly string SurpriseProductName = "Mystery Item";

		#region ICloneable implementation

		public override object Clone ()
		{
			// in practice, this object was already cloned via the AsSurpriseProduct extension method,
			// but overriding Clone here prevents the base method being called and 'de-surprising' our 
			// surprise product.
			return new SurpriseProduct {
				Price = Price,
				Size = Size,
				Name = Name,
				Color = Color,
				ProductType = ProductType,
				Colors = new ProductColor[] { Color }
			};
		}

		#endregion
	}

	public static class ProductExtensions 
	{
		// creates a SurpriseProduct from a Product
		public static SurpriseProduct AsSurpriseProduct(this Product p)
		{
			return new SurpriseProduct {
				Price = p.Price,
				Size =  p.Size,
				Name = p.Name,
				Color = p.Color,
				ProductType = p.ProductType,
				Colors = p.Colors.ToArray(),
				Sizes = p.Sizes.ToArray(),
				Description = p.Description,
			};
		}
	}

	public class Product : ICloneable
	{
		static Random random = new Random ();

		public double Price { get; set; }

		public ProductSize Size { get; set; }

		public ProductColor Color { get; set; }

		public ProductType ProductType { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string[] ImageUrls {
			get { return Colors == null ? new string[0] : Colors.SelectMany (x => x.ImageUrls).ToArray (); }
		}

		int imageIndex = -1;

		public string ImageUrl {
			get {
				if (ImageUrls == null || ImageUrls.Length == 0)
					return "";
				if (ImageUrls.Length == 1)
					return ImageUrls [0];
				if (imageIndex == -1)
					imageIndex = random.Next (ImageUrls.Length);
				return ImageUrls [imageIndex];
			}
		}

		public string ImageForSize (float width)
		{
			return ImageForSize (ImageUrl, width);
		}

		public static string ImageForSize (string url, float width)
		{
			return string.Format ("{0}?width={1}", url, width);
		}

		public string PriceDescription {
			get {
				return Price < 0.01 ? "Free" : Price.ToString ("C");
			}
		}

		public ProductColor[] Colors { get; set; }

		public ProductSize[] Sizes { get; set; }

		#region ICloneable implementation

		public virtual object Clone ()
		{
			return new Product {
				Price =  Price,
				Size =  Size,
				Name = Name,
				Color = Color,
				ProductType = ProductType,
				Colors = new ProductColor[]{
					Color,
				}
			};
		}

		#endregion
	}
}

