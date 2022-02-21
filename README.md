# Azure IoT Demo
This is a .NET Console App demonstrating the basic flow of Azure Device Provisioning Service (DPS), Azure IoT Hub and using Device Twins. This can be run from the console of your development machine, which will enroll the device. But you can also use this app on a Raspberry Pi, as is explained in the last part of the walkthrough.

In this demo we've been using symmetric keys, because that's easier to setup.

## Prerequisites

* An Azure subscription
* An Azure IoT Hub and an Azure Device Provisioning Service (DPS).
  [Quickstart - Set up IoT Hub Device Provisioning Service in the Microsoft Azure portal | Microsoft Docs](https://docs.microsoft.com/en-us/azure/iot-dps/quick-setup-auto-provision)

> It is wise to create a resource group that contains all the resources, so it's easier to delete all resources at once when you're done.

## Create a device enrollment

1. Sign in to the [Azure portal](https://portal.azure.com/).
2. On the left-hand menu or on the portal page, select **All resources**.
3. 
4. In the **Add Enrollment** page, enter the following information.
   - **Mechanism**: Select *Symmetric Key* as the identity attestation Mechanism.
   - **Auto-generate keys**: Check this box.
   - **Registration ID**: Enter a registration ID to identify the enrollment. In this demo we'll be using *prov-device-001*.
   - **IoT Hub Device ID:** Enter a device identifier. In this demo we'll be using *device-001*
   - **Select the IoT hubs this device can be assigned to:** select the IoT Hub you created.![DPS-enroll-device](Images/DPS-enroll-device.png)
5. Select **Save**. A **Primary Key** and **Secondary Key** are generated and added to the enrollment entry, and you are taken back to the **Manage enrollments** page.
6. To view your simulated symmetric key device enrollment, select the **Individual Enrollments** tab.
7. Select your device (*prov-device-001*).
8. Copy the value of the generated **Primary Key**.
   ![Getting the device primary key](Images/dps-device-properties.png)

## Run the AzureIoTDemo Application

You can build the solution in this demo and run it using command line parameters. First you need the **ID Scope** of your DPS Service. To obtains this:

1. In the main menu of your Device Provisioning Service, select **Overview**.
2. Copy the **ID Scope** value.
   ![Copy ID Scope of the DPS instance](Images/dps-id-scope.png)

Other information you need from the previous steps:

* The device ID **prov-device-001**
* The **device primary key** from step 10 in [Create a Device Enrollment](#create-a-device-enrollment)

This is the command line use of the demo with these parameters:

```shell
AzureIoTDemo -s [IdScope] -d prov-device-001 -k [device primary key]
```

When you build the solution, the executable is by default located in the folder `bin\Debug\net6.0`. You can also add the parameters in Visual Studio 2022 via the menu Debug > AzureIoTDemo Debug Properties > Command line arguments and click Run (or press F5).

When the application ran successfully, you can check the automatically setup device in Azure IoT Hub. 

1. Select your Azure IoT Hub.
2. In the **Device Management** menu on the lieft, select **Devices**.

## Reset to run it again

If you want to see DPS in action again, just remove the *device-001* from the Devices section in your Azure IoT Hub.

1. Select your Azure IoT Hub.
2. In the **Device Management** menu on the lieft, select **Devices**.
3. Click the checkbox before **device-001** to select the device

## Explaining what happens

When the application starts
