using Braintree;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public class BrainTreeGate : IBrainTreeGate
    {
        public BrainTreeSender Options { get; set; }
        private IBraintreeGateway BraintreeGateway { get; set; }
        public BrainTreeGate(IOptions<BrainTreeSender>options)
        {
            Options = options.Value;
        }
        public IBraintreeGateway CreateGateway()
        {
            return new BraintreeGateway(Options.Enviorment, Options.MerchantID, Options.PublicKey, Options.PrivateKey);
        }

        public IBraintreeGateway GetGateway()
        {
            if (BraintreeGateway == null)
            {
                BraintreeGateway = CreateGateway();
            }
            return BraintreeGateway;
        }
    }
}
