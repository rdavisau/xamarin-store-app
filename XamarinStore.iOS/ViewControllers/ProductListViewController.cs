using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.Threading.Tasks;
using MonoTouch.CoreAnimation;

namespace XamarinStore.iOS
{
	public class ProductListViewController : UITableViewController
	{
		const int ProductCellRowHeight = 300;
		static float ImageWidth = UIScreen.MainScreen.Bounds.Width * UIScreen.MainScreen.Scale;

		public event Action<Product> ProductTapped = delegate {};

		ProductListViewSource source;
		Random random;

		public ProductListViewController ()
		{

			Title = "Xamarin Store";

			// Hide the back button text when you leave this View Controller.
			NavigationItem.BackBarButtonItem = new UIBarButtonItem ("", UIBarButtonItemStyle.Plain, handler: null);
			TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			TableView.RowHeight = ProductCellRowHeight;
			TableView.Source = source = new ProductListViewSource (
				products => { ProductTapped (products);	}, 
				product=> { SurpriseProductSelected(product); });

			random = new Random ();

			GetData ();
		}

		async void GetData ()
		{
			source.Products = await WebService.Shared.GetProducts ();
			//Kicking off a task no need to await
			#pragma warning disable 4014
			WebService.Shared.PreloadImages (320 * UIScreen.MainScreen.Scale);
			#pragma warning restore 4014
			TableView.ReloadData ();
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			NavigationItem.RightBarButtonItem = AppDelegate.Shared.CreateBasketButton ();
		}

		public void SurpriseProductSelected(Product surprise)
		{
			surprise.Color = surprise.Colors [random.Next(0, surprise.Colors.Count())];   // pick a random colour
			surprise.Size = surprise.Sizes [random.Next (0, surprise.Sizes.Count())];   // pick a random size

			WebService.Shared.CurrentOrder.Add (surprise.AsSurpriseProduct());
			AppDelegate.Shared.UpdateProductsCount();

			new UIAlertView ("Congratulations!", String.Format ("Your surprise will be displayed in your shopping basket under the name '{0}'", SurpriseProduct.SurpriseProductName), null, "Cool!", null).Show ();
		}

		class ProductListViewSource : UITableViewSource
		{
			readonly Action<Product> ProductSelected;
			readonly Action<Product> SurpriseProductSelected;

			Random random;
			UIAlertView alertView;

			public IReadOnlyList<Product> Products;

			public ProductListViewSource (Action<Product> productSelected, Action<Product> surpriseProductSelected)
			{
				ProductSelected = productSelected;
				SurpriseProductSelected = surpriseProductSelected;
				random = new Random();
			}

			public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
			{
				if (indexPath.Section == 1)
					return 190;
				else
					return ProductCellRowHeight;
			}

		    public override int NumberOfSections(UITableView tableView)
		    {
				return (Products == null || Products.Count == 0) ? 1 : 2;
		    }

		    public override int RowsInSection (UITableView tableview, int section)
		    {
		        if (Products == null)
		            return 1;
		        else return (section == 0 ? Products.Count : 1);
		    }

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				if (Products == null)
					return;

			    switch (indexPath.Section)
			    {
			        case 0: // selected a product, continue normally
			            ProductSelected(Products[indexPath.Row]);
			            break;
					case 1: 
						var onSuccess = new Action(() => {

						    var eligibleProducts = Products
							                          .Where(p=> p.Price < 0.01)
							                          .ToList();

						    var surprise = eligibleProducts [random.Next (0, eligibleProducts.Count ())]; // pick a random product 
							SurpriseProductSelected (surprise);
						});

						this.alertView = new UIAlertView ("Are you sure?", 
						                                  "There's no knowing what colour and size it will be, nor which gender it will suit!\n\n(It may not even be a shirt...)", 
							                              new SurpriseAlertViewDelegate(onSuccess),
						                                  "Hm, I'd better not..", 
						                                  new [] { "I'm feeling lucky!" }
						);
						this.alertView.Show ();

				        break;
			    }
			}
				
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				if (Products == null) {
					return new SpinnerCell ();
				}

			    UITableViewCell cell = null;

			    switch (indexPath.Section)
			    {
                    case 0: // normal cell
			            cell = tableView.DequeueReusableCell(ProductListCell.CellId) as ProductListCell ??
			                       new ProductListCell();
					    ((ProductListCell) cell).Product = Products[indexPath.Row];

			            return cell;
				case 1:
					    cell = tableView.DequeueReusableCell (SurpriseMeListCell.CellId) as SurpriseMeListCell ?? new SurpriseMeListCell ();
						var typedCell = cell as SurpriseMeListCell;
						typedCell.NameLabel.Text = "Surprise Me!";
						typedCell.PriceLabel.Text = "free";

			            return cell;
			    }

			    return cell;
			}

			class SurpriseAlertViewDelegate : UIAlertViewDelegate
			{
				readonly Action onSuccess;

				public SurpriseAlertViewDelegate(Action successAction) : base()
				{
					this.onSuccess = successAction;
				}

				public override void Clicked (UIAlertView alertview, int buttonIndex)
				{
					if (buttonIndex == 1) 
						onSuccess ();
				}
			}
		}

		class SurpriseCellBGView : UIView
		{
			public override void Draw(RectangleF rect)
			{
				XamStoreStyleKit.DrawCellProto (rect.Size.Height, rect.Size.Width);
			}

		}

		class SurpriseMeListCell : UITableViewCell
		{
			public const string CellId = "SurpriseMeListCell";
			static readonly SizeF PriceLabelPadding = new SizeF (16, 6);
			Product product;
			SurpriseCellBGView imageView;
			public UILabel NameLabel, PriceLabel;

			UIImageView shirtsImageView;

			public SurpriseMeListCell()
			{
				SelectionStyle = UITableViewCellSelectionStyle.None;
				ContentView.BackgroundColor = UIColor.LightGray;

				imageView = new SurpriseCellBGView { ClipsToBounds = true, };

				shirtsImageView = new UIImageView { Image = UIImage.FromFile("allShirts.png"), BackgroundColor = UIColor.Clear, ContentMode = UIViewContentMode.ScaleAspectFill };

				NameLabel = new UILabel {
					TextColor = UIColor.White,
					TextAlignment = UITextAlignment.Left,
					Font = UIFont.FromName ("HelveticaNeue-Light", 22),
					//ShadowColor = UIColor.DarkGray,
					//ShadowOffset = new System.Drawing.SizeF(.5f,.5f),
					Layer = {
						ShadowRadius = 3,
						ShadowColor = UIColor.Black.CGColor,
						ShadowOffset = new System.Drawing.SizeF(0,1f),
						ShadowOpacity = .5f,
					}
				};

				PriceLabel = new UILabel {
					Alpha = 0.95f,
					TextColor = UIColor.White,
					BackgroundColor = Color.Green,
					TextAlignment = UITextAlignment.Center,
					Font = UIFont.FromName ("HelveticaNeue", 16),
					ShadowColor = UIColor.LightGray,
					ShadowOffset = new SizeF(.5f, .5f),
				};

				var layer = PriceLabel.Layer;
				layer.CornerRadius = 3;


				ContentView.AddSubview (imageView);
				ContentView.AddSubview(shirtsImageView);
				ContentView.AddSubviews (NameLabel, PriceLabel);
			}

			public override void LayoutSubviews ()
			{
				base.LayoutSubviews ();
				var bounds = ContentView.Bounds;

				imageView.Frame = bounds;
				NameLabel.Frame = new RectangleF (
					bounds.X + 12,
					bounds.Bottom - 58,
					bounds.Width,
					55
				);

				var priceSize = ((NSString)"free").StringSize (PriceLabel.Font);
				PriceLabel.Frame = new RectangleF (
					bounds.Width - priceSize.Width - 2 * PriceLabelPadding.Width - 12,
					bounds.Bottom - priceSize.Height - 2 * PriceLabelPadding.Height - 14,
					priceSize.Width + 2 * PriceLabelPadding.Width,
					priceSize.Height + 2 * PriceLabelPadding.Height
				);

				shirtsImageView.Frame = new RectangleF (bounds.X + 10, bounds.Y + 12, bounds.Width - 20, bounds.Height - 50);

			}

	    }

		class ProductListCell : UITableViewCell
		{
			public const string CellId = "ProductListCell";
			static readonly SizeF PriceLabelPadding = new SizeF (16, 6);
			Product product;
			TopAlignedImageView imageView;
			UILabel nameLabel, priceLabel;

			public Product Product {
				get { return product; }
				set {
					product = value;

					nameLabel.Text = product.Name;
					priceLabel.Text = product.PriceDescription.ToLower ();
					updateImage ();
				}
			}

			void updateImage()
			{
				var url = product.ImageForSize (ImageWidth);
				imageView.LoadUrl (url);
			}

			public ProductListCell ()
			{
				SelectionStyle = UITableViewCellSelectionStyle.None;
				ContentView.BackgroundColor = UIColor.LightGray;

				imageView = new TopAlignedImageView {
					ClipsToBounds = true,
				};

				nameLabel = new UILabel {
					TextColor = UIColor.White,
					TextAlignment = UITextAlignment.Left,
					Font = UIFont.FromName ("HelveticaNeue-Light", 22),
					//ShadowColor = UIColor.DarkGray,
					//ShadowOffset = new System.Drawing.SizeF(.5f,.5f),
					Layer = {
						ShadowRadius = 3,
						ShadowColor = UIColor.Black.CGColor,
						ShadowOffset = new System.Drawing.SizeF(0,1f),
						ShadowOpacity = .5f,
					}
				};

				priceLabel = new UILabel {
					Alpha = 0.95f,
					TextColor = UIColor.White,
					BackgroundColor = Color.Green,
					TextAlignment = UITextAlignment.Center,
					Font = UIFont.FromName ("HelveticaNeue", 16),
					ShadowColor = UIColor.LightGray,
					ShadowOffset = new SizeF(.5f, .5f),
				};

				var layer = priceLabel.Layer;
				layer.CornerRadius = 3;

				ContentView.AddSubviews (imageView, nameLabel, priceLabel);
			}

			public override void LayoutSubviews ()
			{
				base.LayoutSubviews ();
				var bounds = ContentView.Bounds;

				imageView.Frame = bounds;
				nameLabel.Frame = new RectangleF (
					bounds.X + 12,
					bounds.Bottom - 58,
					bounds.Width,
					55
				);

				var priceSize = ((NSString)Product.PriceDescription).StringSize (priceLabel.Font);
				priceLabel.Frame = new RectangleF (
					bounds.Width - priceSize.Width - 2 * PriceLabelPadding.Width - 12,
					bounds.Bottom - priceSize.Height - 2 * PriceLabelPadding.Height - 14,
					priceSize.Width + 2 * PriceLabelPadding.Width,
					priceSize.Height + 2 * PriceLabelPadding.Height
				);
			}
		}
	}
}