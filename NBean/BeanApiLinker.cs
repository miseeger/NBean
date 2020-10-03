namespace NBean
{
    internal class BeanApiLinker : BeanObserver
    {

        private readonly BeanApi _api;

        public BeanApiLinker(BeanApi api)
        {
            _api = api;
        }


        public override void AfterDispense(Bean bean)
        {
            bean.Api = _api;
        }
    }
}
