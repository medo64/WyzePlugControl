using System;

namespace WyzeApi {
    /// <summary>
    /// Wyze API plug device.
    /// </summary>
    public class WyzePlugDevice : WyzeDevice {

        internal WyzePlugDevice(Wyze wyze, string mac, string nickname, string productModel, string productType)
            : base(wyze, mac, nickname, productModel, productType) {
        }


        /// <summary>
        /// Sets plug power state.
        /// </summary>
        /// <param name="newState">New state.</param>
        public void SetPowerState(bool newState){
            Wyze.SetPlugPowerState(this, newState);
        }

    }
}
