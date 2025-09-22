namespace burbodek.Services
{
    public interface IPaymongo
    {
        public Task<string> CreateCheckoutSession(
           decimal amount,
           string currency,
           string name,
           string email,
           string contact,
           string productNames);
        public Task<string> GetCheckoutSession(string sessionId);
        public void Dispose();
    }
}