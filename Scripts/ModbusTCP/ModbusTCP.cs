using System;
using System.Net.Sockets;
using NModbus;
using NModbus.Device;
using NModbus.Utility;


namespace Test.ModbusTCP
{
    public class ModbusTcpClient
    {
        private string ipAddress;
        private int port;
        private TcpClient tcpClient;
        private IModbusMaster modbusMaster;

        public ModbusTcpClient(string ipAddress, int port = 502)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            tcpClient = new TcpClient(ipAddress, port);

            // 使用 NModbus 的工厂方法创建 Modbus 主站
            var factory = new ModbusFactory();
            modbusMaster = factory.CreateMaster(tcpClient);
        }
        /// <summary>
        /// read sigle MW (Memory Word) 16 bits register
        /// </summary>
        /// <param name="slaveId">slave id</param>
        /// <param name="address">start addrees</param>
        public ushort ReadMW(byte slaveId, ushort address)
        {
            try
            {
                // read single holding register (MW)
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, address, 1);
                return registers[0];  // return 16 bits register value
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading MW: " + ex.Message);
                return 0;             // exception return 0
            }
        }

        /// <summary>
        /// write sigle MW (Memory Word) 16 bits register
        /// </summary>
        /// <param name="slaveId">slave id</param>
        /// <param name="address">start address</param>
        /// <param name="value">vlaue to write</param>
        public void WriteMW(byte slaveId, ushort address, ushort value)
        {
            try
            {
                // write sigle holding register (MW)
                modbusMaster.WriteSingleRegister(slaveId, address, value);
                Console.WriteLine("Successfully write to MW address " + address);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing MW: " + ex.Message);
            }
        }

        // read multiple MW (multi 16 bits register)
        public ushort[] ReadMultipleMW(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            try
            {
                return modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading multiple MW: " + ex.Message);
                return null;
            }
        }

        // write multiple MW (multi 16 bits register)
        public void WriteMultipleMW(byte slaveId, ushort startAddress, ushort[] values)
        {
            try
            {
                modbusMaster.WriteMultipleRegisters(slaveId, startAddress, values);
                Console.WriteLine("Successfully wrote multiple MW registers.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing multiple MW registers: " + ex.Message);
            }
        }

        // 读取多个 MW 并转换为 int (小端序)
        public int ReadMultipleMWAsIntLittleEndian(byte slaveId, ushort startAddress)
        {
            try
            {
                // 读取 2 个保持寄存器 (32 位整数)
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, startAddress, 2);

                // 将 2 个 16 位寄存器组合成 32 位 int (小端序)
                int result = (registers[1] << 16) | registers[0];

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading multiple MW: " + ex.Message);
                return 0;
            }
        }

        // 读取多个 MW 并转换为 int (大端序)
        public int ReadMultipleMWAsIntBigEndian(byte slaveId, ushort startAddress)
        {
            try
            {
                // 读取 2 个保持寄存器 (32 位整数)
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, startAddress, 2);

                // 将 2 个 16 位寄存器组合成 32 位 int (大端序)
                // registers[0] 是高 16 位, registers[1] 是低 16 位
                int result = (registers[0] << 16) | registers[1];

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading multiple MW: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// read coil（Coils）
        /// </summary>
        /// <param name="slaveAddress">slave id</param>
        /// <param name="startAddress">start address</param>
        /// <param name="numberOfPoints">coil numbers to read</param>
        /// <returns>coil bool array</returns>
        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return modbusMaster.ReadCoils(slaveAddress, startAddress, numberOfPoints);
        }

        /// <summary>
        /// read holding register（Holding Registers）
        /// </summary>
        /// <param name="slaveAddress">slave id</param>
        /// <param name="startAddress">start address</param>
        /// <param name="numberOfPoints">number of register to read</param>
        /// <returns>return value in register</returns>
        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            return modbusMaster.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints);
        }

        /// <summary>
        /// write sigle coil（Coil）
        /// </summary>
        /// <param name="slaveAddress">slave id</param>
        /// <param name="coilAddress">coil address</param>
        /// <param name="value">coil bool value</param>
        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            modbusMaster.WriteSingleCoil(slaveAddress, coilAddress, value);
        }

        /// <summary>
        /// write sigle holding register（Holding Register）
        /// </summary>
        /// <param name="slaveAddress">slave id</param>
        /// <param name="registerAddress">register address</param>
        /// <param name="value">value to write</param>
        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            modbusMaster.WriteSingleRegister(slaveAddress, registerAddress, value);
        }

        /// <summary>
        /// 读取双精度浮点数（Double）
        /// 由于双精度浮点数占用 64 位，因此需要读取 4 个寄存器（每个寄存器 16 位）
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <returns>返回读取的双精度浮点数</returns>
        public double ReadDouble(byte slaveAddress, ushort startAddress)
        {
            // 读取4个寄存器（64位 = 4 * 16位寄存器）
            ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveAddress, startAddress, 4);

            // 将4个16位寄存器转换为双精度浮点数
            return ConvertRegistersToDouble(registers);
        }

        /// <summary>
        /// 写入双精度浮点数（Double）
        /// 需要将双精度浮点数拆分为4个16位寄存器来进行写入
        /// </summary>
        /// <param name="slaveAddress">从站地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="value">要写入的双精度浮点数</param>
        public void WriteDouble(byte slaveAddress, ushort startAddress, double value)
        {
            // 将双精度浮点数转换为4个16位寄存器
            ushort[] registers = ConvertDoubleToRegisters(value);

            // 写入4个寄存器
            modbusMaster.WriteMultipleRegisters(slaveAddress, startAddress, registers);
        }

        /// <summary>
        /// 将 Modbus 寄存器数组转换为双精度浮点数
        /// </summary>
        private double ConvertRegistersToDouble(ushort[] registers)
        {
            if (registers.Length != 4)
                throw new ArgumentException("Must have 4 registers to convert to a double.");

            // 将两个16位整数组合为64位整数
            byte[] bytes = new byte[8];
            bytes[0] = (byte)(registers[1] >> 8);
            bytes[1] = (byte)(registers[1]);
            bytes[2] = (byte)(registers[0] >> 8);
            bytes[3] = (byte)(registers[0]);
            bytes[4] = (byte)(registers[3] >> 8);
            bytes[5] = (byte)(registers[3]);
            bytes[6] = (byte)(registers[2] >> 8);
            bytes[7] = (byte)(registers[2]);

            // 将字节数组转换为双精度浮点数
            return BitConverter.ToDouble(bytes, 0);
        }

        public double ReadDoubleBigEndian(byte slaveId, ushort startAddress)
        {
            try
            {
                // 读取 4 个寄存器 (64 位 double)
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, startAddress, 4);

                // 将 4 个寄存器转换为字节数组 (大端序)
                byte[] bytes = new byte[8];
                bytes[7] = (byte)(registers[0]);
                bytes[6] = (byte)(registers[0] >> 8);
                bytes[5] = (byte)(registers[1]);
                bytes[4] = (byte)(registers[1] >> 8);
                bytes[3] = (byte)(registers[2]);
                bytes[2] = (byte)(registers[2] >> 8);
                bytes[1] = (byte)(registers[3]);
                bytes[0] = (byte)(registers[3] >> 8);

                // 将字节数组转换为 double
                return BitConverter.ToDouble(bytes, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading double (Big-endian): " + ex.Message);
                return double.NaN; // 返回 NaN 表示错误
            }
        }

        // 写入 double 类型数据 (写入到 4 个保持寄存器，按大端序)
        public void WriteDoubleBigEndian(byte slaveId, ushort startAddress, double value)
        {
            try
            {
                // 将 double 转换为字节数组
                byte[] bytes = BitConverter.GetBytes(value);

                // 按大端序将字节数组转换为 4 个 16 位寄存器
                ushort[] registers = new ushort[4];
                registers[3] = (ushort)((bytes[1] << 8) | bytes[0]);
                registers[2] = (ushort)((bytes[3] << 8) | bytes[2]);
                registers[1] = (ushort)((bytes[5] << 8) | bytes[4]);
                registers[0] = (ushort)((bytes[7] << 8) | bytes[6]);

                // 写入 4 个寄存器
                modbusMaster.WriteMultipleRegisters(slaveId, startAddress, registers);
                Console.WriteLine("Successfully wrote double value (Big-endian).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing double (Big-endian): " + ex.Message);
            }
        }

        // 读取 double 类型数据 (从 4 个保持寄存器中读取，按小端序)
        public double ReadDoubleLittleEndian(byte slaveId, ushort startAddress)
        {
            try
            {
                // 读取 4 个寄存器 (64 位 double)
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, startAddress, 4);

                // 将 4 个寄存器转换为字节数组 (小端序)
                byte[] bytes = new byte[8];
                bytes[0] = (byte)(registers[0]);
                bytes[1] = (byte)(registers[0] >> 8);
                bytes[2] = (byte)(registers[1]);
                bytes[3] = (byte)(registers[1] >> 8);
                bytes[4] = (byte)(registers[2]);
                bytes[5] = (byte)(registers[2] >> 8);
                bytes[6] = (byte)(registers[3]);
                bytes[7] = (byte)(registers[3] >> 8);

                // 将字节数组转换为 double
                return BitConverter.ToDouble(bytes, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading double: " + ex.Message);
                return double.NaN; // 返回 NaN 表示错误
            }
        }

        // 写入 double 类型数据 (写入到 4 个保持寄存器，按小端序)
        public void WriteDoubleLittleEndian(byte slaveId, ushort startAddress, double value)
        {
            try
            {
                // 将 double 转换为字节数组
                byte[] bytes = BitConverter.GetBytes(value);

                // 将字节数组按小端序分解为 4 个 16 位寄存器
                ushort[] registers = new ushort[4];
                registers[0] = (ushort)((bytes[0]) | (bytes[1] << 8));
                registers[1] = (ushort)((bytes[2]) | (bytes[3] << 8));
                registers[2] = (ushort)((bytes[4]) | (bytes[5] << 8));
                registers[3] = (ushort)((bytes[6]) | (bytes[7] << 8));

                // 写入 4 个寄存器
                modbusMaster.WriteMultipleRegisters(slaveId, startAddress, registers);
                Console.WriteLine("Successfully wrote double value.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing double: " + ex.Message);
            }
        }

        /// <summary>
        /// 将双精度浮点数转换为 Modbus 寄存器数组
        /// </summary>
        private ushort[] ConvertDoubleToRegisters(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            ushort[] registers = new ushort[4];
            registers[0] = BitConverter.ToUInt16(new byte[] { bytes[2], bytes[3] }, 0);
            registers[1] = BitConverter.ToUInt16(new byte[] { bytes[0], bytes[1] }, 0);
            registers[2] = BitConverter.ToUInt16(new byte[] { bytes[6], bytes[7] }, 0);
            registers[3] = BitConverter.ToUInt16(new byte[] { bytes[4], bytes[5] }, 0);

            return registers;
        }

        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        public void Disconnect()
        {
            tcpClient.Close();
        }
    }

}
