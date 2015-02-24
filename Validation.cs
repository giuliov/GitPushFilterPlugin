namespace GitPushFilter
{
    internal class Validation
    {
        public Validation()
        {
            Fails = false;
            ReasonCode = 0;
            ReasonMessage = string.Empty;
        }

        public bool Fails { get; set; }

        public int ReasonCode { get; set; }

        public string ReasonMessage { get; set; }
    }
}