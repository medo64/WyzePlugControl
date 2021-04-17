using System;

namespace WyzeApi {
    /// <summary>
    /// Wyze API device.
    /// </summary>
    public class WyzeDevice {

        internal WyzeDevice(Wyze wyze, string mac, string nickname, string productModel, string productType) {
            Wyze = wyze;
            Id = mac;
            Nickname = nickname;
            ProductModel = productModel;
            ProductType = productType;
        }


        protected Wyze Wyze { get; init; }

        /// <summary>
        /// Gets unique identifier.
        /// Usually MAC address.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Gets user-defined device name.
        /// </summary>
        public string Nickname { get; init; }

        /// <summary>
        /// Gets product model.
        /// </summary>
        public string ProductModel { get; init; }

        /// <summary>
        /// Gets product type.
        /// </summary>
        public string ProductType { get; init; }

    }
}
