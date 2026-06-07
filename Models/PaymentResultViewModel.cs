namespace LanguageCenter.Models
{
    public class PaymentResultViewModel
    {
        public bool IsSuccess { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int? PaymentID { get; set; }
        public string Amount { get; set; }
        public string TransactionNo { get; set; }
        public string ResponseCode { get; set; }
    }
}
