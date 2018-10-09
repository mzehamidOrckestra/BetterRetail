///<reference path='../../../../Composer.UI/Source/Typings/tsd.d.ts' />
///<reference path='../../../../Composer.UI/Source/TypeScript/Mvc/Controller.ts' />
///<reference path='../../../../Composer.UI/Source/TypeScript/Mvc/IControllerActionContext.ts' />
///<reference path='../../../../Composer.Cart.UI/WishList/Source/Typescript/WishListRepository.ts' />
///<reference path='../../../../Composer.Cart.UI/WishList/Source/Typescript/Services/WishListService.ts' />
///<reference path='../../../../Composer.Cart.UI/CartSummary/Source/Typescript/CartService.ts' />
///<reference path='../../../../Composer.UI/Source/TypeScript/Repositories/CartRepository.ts' />

module Orckestra.Composer {

    export class WishListController extends Orckestra.Composer.Controller {

        protected _wishListService: IWishListService = new WishListService(new WishListRepository(), this.eventHub);
        protected _cartService: CartService = new CartService(new CartRepository(), this.eventHub);

        public initialize() {

            super.initialize();
        }

        public addToCart(actionContext: IControllerActionContext) {
            var context: JQuery = actionContext.elementContext;
            var container = context.closest('.wishlist-tile');
            var productId: string = <any>context.data('productid');
            var price: string = <any>context.data('price');
            var brand: string = <any>context.data('brand');
            var variantId: string = <any>context.data('variantid');
            var variant: string = <any>context.data('variant');
            var name: string = <any>context.data('name');
            var category: string = <any>context.data('category');

            this.eventHub.publish('wishListLineItemAddingToCart', {
                data: this.getProductDataForAnalytics(productId, variant, name, price, brand, category)
            });

            container.addClass('is-loading');

            this._cartService.addLineItem(productId, price, variantId, 1)
                .fin(() => container.removeClass('is-loading'));

        }


        protected getListNameForAnalytics(): string {
            throw new Error('ListName not defined for this controller');
        }

        protected getProductDataForAnalytics(productId, variant, displayName, price, brand, category): any {

            var data = {
                List: this.getListNameForAnalytics(),
                ProductId: productId,
                Variant: variant,
                DisplayName: displayName,
                ListPrice: price,
                Brand: brand,
                CategoryId: category,
                Quantity: 1
            };

            return data;
        }
    }
}