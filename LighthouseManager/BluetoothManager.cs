﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using LighthouseManager.Helper;

namespace LighthouseManager
{
    /// <summary>
    ///     Managing all Bluetooth related things
    /// </summary>
    public class BluetoothManager : IDisposable
    {
        private readonly string _pwrService = "00001523-1212-EFDE-1523-785FEABCD124";
        private BluetoothLEAdvertisementWatcher AdvertisementWatcher { get; set; }
        private List<BluetoothLEDevice> BluetoothLeDevices { get; } = new();
        private List<ulong> Basestations { get; } = new();

        public void Dispose()
        {
            BluetoothLeDevices.ForEach(x => x.Dispose());
        }

        /// <summary>
        ///     Initializing new BluetoothLEAdvertisementWatcher and listening for devices
        /// </summary>
        public void StartWatcher()
        {
            AdvertisementWatcher = new BluetoothLEAdvertisementWatcher {ScanningMode = BluetoothLEScanningMode.Active};
            AdvertisementWatcher.Received += WatcherOnReceived;

            try
            {
                AdvertisementWatcher.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///     Stop listening for devices and clearing collected basestations
        /// </summary>
        public void StopWatcher()
        {
            if (AdvertisementWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                AdvertisementWatcher.Stop();

            Basestations.Clear();
        }

        private void WatcherOnReceived(BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Filtering for basestations, should begin with "LHB-"
            if (!args.Advertisement.LocalName.StartsWith("LHB-")) return;

            if (Basestations.All(x => x != args.BluetoothAddress))
            {
                Basestations.Add(args.BluetoothAddress);

                Console.WriteLine(
                    $"Potential Base Station found. Name: {args.Advertisement.LocalName}, Bluetooth Address: {args.BluetoothAddress.ToMacString()}.");
            }
        }

        /// <summary>
        ///     Changing the power state of given basestation addresses
        /// </summary>
        /// <param name="address">Bluetooth-Addresse</param>
        /// <param name="powerState">Selected power state</param>
        public async Task ChangePowerstate(ulong address, Powerstate powerState)
        {
            BluetoothLEDevice device = null;


            Console.WriteLine($"{address.ToMacString()}: Connecting to device.");
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            if (device == null) Console.WriteLine($"{address.ToMacString()}: Failed to connect to device.");

            if (device != null)
            {
                if (BluetoothLeDevices.All(x => x.BluetoothAddress != device.BluetoothAddress))
                    BluetoothLeDevices.Add(device);

                Console.WriteLine($"{address.ToMacString()}: Trying to get Gatt services and characteristics.");
                var gattServiceResult = await device.GetGattServicesAsync();

                if (gattServiceResult.Status == GattCommunicationStatus.Success)
                {
                    var characteristicsResult = await gattServiceResult.Services
                        .Single(s => s.Uuid == Guid.Parse(_pwrService))
                        .GetCharacteristicsAsync();

                    if (characteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        var powerstate = new Models.Characteristics.Powerstate();

                        var characteristic =
                            characteristicsResult.Characteristics.Single(c =>
                                c.Uuid == powerstate.GetGuid());

                        GattWriteResult result;

                        switch (powerState)
                        {
                            case Powerstate.Wake:
                                result = await WriteAsync(characteristic, Models.Characteristics.Powerstate.Wake);
                                break;
                            case Powerstate.Sleep:
                                result = await WriteAsync(characteristic, Models.Characteristics.Powerstate.Sleep);
                                break;
                            case Powerstate.Standby:
                                result = await WriteAsync(characteristic,
                                    Models.Characteristics.Powerstate.Standby);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(powerState), powerState, null);
                        }

                        if (result.Status == GattCommunicationStatus.Success)
                        {
                            Console.WriteLine(
                                $"{address.ToMacString()}: Successfully executed '{powerState}' command.");
                        }
                        else
                        {
                            Console.WriteLine($"{address.ToMacString()}: Execution failed: {result.Status}.");
                            device.Dispose();
                            throw new GattCommunicationException($"{address.ToMacString()} WriteValueWithResultAsyncError",
                                gattServiceResult.Status);
                        }
                    }
                    else
                    {
                        device.Dispose();
                        throw new GattCommunicationException($"{address.ToMacString()} GetCharacteristicsAsyncError",
                            gattServiceResult.Status);
                    }
                }
                else
                {
                    device.Dispose();
                    throw new GattCommunicationException($"{address.ToMacString()} GetGattServicesAsyncError",
                        gattServiceResult.Status);
                }
            }

            device?.Dispose();
        }

        private async Task<GattWriteResult> WriteAsync(GattCharacteristic characteristic, byte value)
        {
            var writer = new DataWriter();
            writer.WriteByte(value);
            return await characteristic.WriteValueWithResultAsync(writer.DetachBuffer());
        }
    }
}