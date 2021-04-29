namespace NginxGUI.Dtos
{
    public class CertificateRequestForm
    {
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Organisation { get; set; }
        public string Department { get; set; }
        public string CommonName { get; set; }
        public string EmailAddress { get; set; }

        public string Build() => $"/C={Country}/ST={State}/L={City}/O={Organisation}/OU={Department}/CN={CommonName}/emailAddress={EmailAddress}";
    }
}