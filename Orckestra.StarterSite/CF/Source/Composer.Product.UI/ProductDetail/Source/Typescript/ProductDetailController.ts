///<reference path='../../../Product/Source/Typescript/ProductController.ts' />
///<reference path='../../../../Composer.Cart.UI/RecurringOrder/source/TypeScript/Repositories/RecurringOrderRepository.ts' />

module Orckestra.Composer {
    export class ProductDetailController extends Orckestra.Composer.ProductController {

        protected _concern: string = 'productDetail';

        private availableFrequencies = [];
        private selectedRecurringOrderFrequencyName: string;
        private recurringMode: string = 'single';

        public initialize() {

            super.initialize();

            this.productService.updateSelectedKvasWith(this.context.viewModel.selectedKvas, this._concern);

            var priceDisplayBusy: UIBusyHandle = this.asyncBusy({
                msDelay: 300,
                loadingIndicatorSelector: '.loading-indicator-pricediscount'
            });

            var addToCartBusy: UIBusyHandle = this.asyncBusy({
                msDelay: 300,
                loadingIndicatorSelector: '.loading-indicator-inventory'
            });

            Q.when(this.calculatePrice()).done(() => {
                priceDisplayBusy.done();

                this.notifyAnalyticsOfProductDetailsImpression();
            });
            Q.when(this.renderData()).done(() => addToCartBusy.done());
        }

        protected getListNameForAnalytics(): string {
            return 'Detail';
        }

        protected notifyAnalyticsOfProductDetailsImpression() {
            var vm = this.context.viewModel;
            var variant: any = _.find(vm.allVariants, (v: any) => v.Id === vm.selectedVariantId);

            var data: any = this.getProductDataForAnalytics(vm);
            if (variant) {
                var variantData: any = this.getVariantDataForAnalytics(variant);

                _.extend(data, variantData);
            }

            this.publishProductImpressionEvent(data);
        }

        protected publishProductImpressionEvent(data: any) {
            this.eventHub.publish('productDetailsRendered',
                {
                    data: data
                });
        }

        protected onSelectedVariantIdChanged(e: IEventInformation) {

            this.renderData().done();
        }

        protected onSelectedKvasChanged(e: IEventInformation) {

            this.render('KvaItems', {KeyVariantAttributeItems: e.data});
        }

        protected onImagesChanged(e: IEventInformation) {

            if (this.isProductWithVariants() && this.isSelectedVariantUnavailable()) {
                this.render('ProductImages', this.getUnavailableProductImages(e));
            } else {
                this.render('ProductImages', e.data);
            }
        }

        private getUnavailableProductImages(e: IEventInformation): any {

            var fallbackImageUrl: string = e.data.FallbackImageUrl;

            var image: any = {
                ThumbnailUrl: fallbackImageUrl,
                ImageUrl: fallbackImageUrl,
                Selected: true
            };

            var vm: any = {
                DisplayName: e.data.DisplayName,
                Images: [image],
                SelectedImage: {
                    ImageUrl: fallbackImageUrl
                }
            };

            return vm;
        }

        protected onPricesChanged(e: IEventInformation) {

            if (this.isProductWithVariants() && this.isSelectedVariantUnavailable()) {
                this.render('PriceDiscount', null);
            } else {
                this.render('PriceDiscount', e.data);
            }
        }

        protected renderUnavailableAddToCart(): Q.Promise<void> {

            return Q.fcall(() => this.render('AddToCartProductDetail', {IsUnavailable: true}));
        }

        protected renderAvailableAddToCart(): Q.Promise<void> {

            var sku: string = this.context.viewModel.Sku;

            return this.inventoryService.isAvailableToSell(sku)
                .then(result => {
                    this.render('AddToCartProductDetail', {IsAvailableToSell: result});
                    this.renderRecurringAddToCartProductDetailFrequency();
                });
        }

        public selectKva(actionContext: IControllerActionContext) {

            super.selectKva(actionContext);

            //IE8 check
            if (history) {
                this.productService.replaceHistory();
            }
        }

        protected completeAddLineItem(quantityAdded: any): Q.Promise<void> {

            var quantity = {
                Min: quantityAdded.Min,
                Max: quantityAdded.Max,
                Value: quantityAdded.Min
            };

            return this.renderAvailableQuantity(quantity);
        }

        private renderRecurringAddToCartProductDetailFrequency() {
            let loc = LocalizationProvider.instance(),
                recurringBubblePitch = ((loc.getLocalizedString('ProductPage', 'L_RecurringBubblePitch'))),
                availableFrequencies = this.context.viewModel.RecurringOrderFrequencies || [];

            if (!this.selectedRecurringOrderFrequencyName && availableFrequencies.length > 0) {
                this.selectedRecurringOrderFrequencyName = availableFrequencies[0].RecurringOrderFrequencyName;
            }

            this.inventoryService.isAvailableToSell(this.context.viewModel.Sku)
                .then(result => {
                    if (this.context.viewModel.IsRecurringOrderEligible && result) {
                        this.render('ProductRecurringFrequency', {
                            recurringMode: this.recurringMode,
                            isAvailableForRecurring: this.context.viewModel.IsRecurringOrderEligible,
                            availableFrequencies: availableFrequencies.map(freq => ({
                                recurringOrderFrequencyName: freq.RecurringOrderFrequencyName,
                                displayName: freq.DisplayName
                            })),
                            selectedRecurringOrderFrequencyName: this.selectedRecurringOrderFrequencyName,
                            recurringBubblePitch: recurringBubblePitch
                        });
                    }
                });
        }

        public onRecurringOrderFrequencySelectChanged(actionContext: IControllerActionContext) {
            let element = <any>actionContext.elementContext[0],
                option = element.options[element.selectedIndex];

            if (option) {
                this.selectedRecurringOrderFrequencyName = option.value === '' ? null : option.value;
            }
        }

        public changeRecurringMode(actionContext: IControllerActionContext) {
            let container$ = actionContext.elementContext.closest('.js-recurringModes');
            container$.find('.js-recurringModeRow.selected').removeClass('selected');
            actionContext.elementContext.closest('.js-recurringModeRow').addClass('selected');
            $('.modeSelection').collapse('toggle');
            this.recurringMode = actionContext.elementContext.val();
        }

        public addToCartButtonClick(actionContext: IControllerActionContext) {
            let frequencyName = this.recurringMode === 'single' ? null : this.selectedRecurringOrderFrequencyName;
            if (frequencyName === null) {
                this.addLineItem(actionContext);
            } else {
                this.addLineItem(actionContext, frequencyName, this.context.viewModel.RecurringOrderProgramName);
            }
        }
    }
}
