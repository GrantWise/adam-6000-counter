#!/usr/bin/env python3
"""
Simple Modbus TCP connection test to verify simulator connectivity
Tests direct connection to adam-simulator-1 on port 5502
"""

import socket
import struct
import time
import sys

def create_modbus_read_request(unit_id=1, start_address=0, register_count=2):
    """Create a Modbus TCP read holding registers request"""
    # Modbus TCP header
    transaction_id = 0x0001
    protocol_id = 0x0000
    length = 6  # Unit ID + Function Code + Start Address + Register Count
    
    # Modbus PDU
    function_code = 0x03  # Read Holding Registers
    
    # Pack the request
    request = struct.pack('>HHHBHH', 
                         transaction_id,
                         protocol_id, 
                         length,
                         unit_id,
                         function_code,
                         start_address)
    request += struct.pack('>H', register_count)
    
    return request

def parse_modbus_response(response):
    """Parse a Modbus TCP response"""
    if len(response) < 9:
        return None, f"Response too short: {len(response)} bytes"
    
    # Parse header
    transaction_id, protocol_id, length, unit_id, function_code = struct.unpack('>HHHBB', response[:8])
    
    if function_code & 0x80:  # Error response
        error_code = response[8]
        return None, f"Modbus error: Function {function_code & 0x7F}, Error {error_code}"
    
    if function_code == 0x03:  # Read Holding Registers response
        byte_count = response[8]
        if len(response) < 9 + byte_count:
            return None, "Incomplete response data"
        
        registers = []
        for i in range(0, byte_count, 2):
            reg_value = struct.unpack('>H', response[9 + i:11 + i])[0]
            registers.append(reg_value)
        
        return registers, None
    
    return None, f"Unexpected function code: {function_code}"

def test_modbus_connection(host='adam-simulator-1', port=5502, unit_id=1):
    """Test Modbus TCP connection to simulator"""
    print(f"Testing Modbus connection to {host}:{port} (Unit ID: {unit_id})")
    
    try:
        # Create socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(5.0)
        
        print(f"Connecting to {host}:{port}...")
        sock.connect((host, port))
        print("TCP connection established")
        
        # Test reading registers 0-1 (32-bit counter for channel 0)
        print("Reading holding registers 0-1...")
        request = create_modbus_read_request(unit_id, 0, 2)
        
        print(f"Sending request: {request.hex()}")
        sock.send(request)
        
        # Receive response
        response = sock.recv(1024)
        print(f"Received response: {response.hex()}")
        
        # Parse response
        registers, error = parse_modbus_response(response)
        if error:
            print(f"Error parsing response: {error}")
            return False
        
        print(f"Successfully read registers: {registers}")
        
        # Convert to 32-bit counter value (low word, high word)
        if len(registers) >= 2:
            counter_value = registers[1] << 16 | registers[0]
            print(f"Channel 0 counter value: {counter_value}")
        
        sock.close()
        return True
        
    except socket.timeout:
        print("Connection timeout")
        return False
    except socket.gaierror as e:
        print(f"DNS resolution failed: {e}")
        return False
    except ConnectionRefusedError:
        print("Connection refused - simulator may not be running")
        return False
    except Exception as e:
        print(f"Connection failed: {e}")
        return False
    finally:
        try:
            sock.close()
        except:
            pass

def test_multiple_unit_ids(host='adam-simulator-1', port=5502):
    """Test different Unit IDs to see which one the simulator responds to"""
    print(f"\nTesting different Unit IDs on {host}:{port}:")
    
    for unit_id in [0, 1, 2, 255]:
        print(f"\n--- Testing Unit ID {unit_id} ---")
        success = test_modbus_connection(host, port, unit_id)
        if success:
            print(f"✓ Unit ID {unit_id} works!")
        else:
            print(f"✗ Unit ID {unit_id} failed")

if __name__ == "__main__":
    print("=== Modbus Connection Test ===")
    
    # Parse command line arguments
    host = 'adam-simulator-1'
    port = 5502
    
    if len(sys.argv) >= 2:
        host = sys.argv[1]
    if len(sys.argv) >= 3:
        port = int(sys.argv[2])
    
    # Test basic connection with Unit ID 1 (what the logger uses)
    success = test_modbus_connection(host, port)
    
    if not success:
        print("\nBasic test failed. Testing different Unit IDs...")
        test_multiple_unit_ids(host, port)
    else:
        print("\n✓ Basic Modbus connection test passed!")
    
    print("\n=== Test Complete ===")