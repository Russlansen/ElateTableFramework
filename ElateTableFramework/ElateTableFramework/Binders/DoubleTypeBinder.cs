using System;
using System.Web.Mvc;

namespace ElateTableFramework.Binders
{
    public class DoubleTypeBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            var value = valueProviderResult.AttemptedValue.Replace(".", ",");

            return valueProviderResult == null ? base.BindModel(controllerContext, bindingContext) :
                                                 Double.Parse(value);
        }
    }
}
