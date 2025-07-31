#!/usr/bin/env python3
"""
Simple Modbus TCP client to test the ADAM-6051 simulator
"""
import sys
import time

try:
    from pymodbus.client import ModbusTcpClient
    from pymodbus.exceptions import ModbusException
except ImportError:
    print("Please install pymodbus: pip install pymodbus")
    sys.exit(1)

def test_adam_simulator(host='localhost', port=5502):
    """Test the ADAM-6051 simulator"""
    print(f"Connecting to ADAM-6051 simulator at {host}:{port}")
    
    client = ModbusTcpClient(host=host, port=port)
    
    try:
        # Connect to the server
        if not client.connect():
            print("Failed to connect to Modbus server")
            return
        
        print("Connected successfully!")
        
        # Read counter values (holding registers)
        # ADAM-6051 has 16 channels, each counter is 32-bit (2 registers)
        print("\nReading counter values:")
        
        for channel in range(16):
            # Each counter uses 2 registers (32-bit value)
            base_address = channel * 2
            result = client.read_holding_registers(address=base_address, count=2, slave=1)
            
            if not result.isError():
                # Combine the two 16-bit registers into a 32-bit counter value
                low_word = result.registers[0]
                high_word = result.registers[1]
                counter_value = (high_word << 16) | low_word
                print(f"Channel {channel}: {counter_value}")
            else:
                print(f"Channel {channel}: Error reading - {result}")
        
        # Read digital input status
        print("\nReading digital input status:")
        result = client.read_holding_registers(address=64, count=16, slave=1)
        
        if not result.isError():
            for i, status in enumerate(result.registers):
                print(f"DI Channel {i}: {'ON' if status else 'OFF'}")
        else:
            print(f"Error reading DI status: {result}")
        
        # Monitor counter changes
        print("\nMonitoring counter changes (press Ctrl+C to stop):")
        previous_values = {}
        
        while True:
            for channel in range(3):  # Monitor first 3 channels
                base_address = channel * 2
                result = client.read_holding_registers(address=base_address, count=2, slave=1)
                
                if not result.isError():
                    low_word = result.registers[0]
                    high_word = result.registers[1]
                    counter_value = (high_word << 16) | low_word
                    
                    if channel not in previous_values:
                        previous_values[channel] = counter_value
                    
                    if counter_value != previous_values[channel]:
                        delta = counter_value - previous_values[channel]
                        print(f"Channel {channel}: {counter_value} (Δ{delta:+d})")
                        previous_values[channel] = counter_value
            
            time.sleep(1)
            
    except KeyboardInterrupt:
        print("\nMonitoring stopped by user")
    except ModbusException as e:
        print(f"Modbus error: {e}")
    except Exception as e:
        print(f"Error: {e}")
    finally:
        client.close()
        print("Disconnected from server")

if __name__ == "__main__":
    test_adam_simulator()