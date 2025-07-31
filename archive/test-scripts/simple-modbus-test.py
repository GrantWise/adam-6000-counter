#!/usr/bin/env python3

import socket
import struct
import time

def simple_modbus_test(host, port):
    print(f"Simple Modbus test to {host}:{port}")
    
    try:
        # Create socket connection
        sock = socket.create_connection((host, port), timeout=5)
        print("TCP connection established")
        
        # Build basic Modbus TCP request
        # Function code 03 (Read Holding Registers), Unit ID 1, Read 2 registers starting at address 0
        transaction_id = 1
        protocol_id = 0
        length = 6
        unit_id = 1
        function_code = 3
        start_address = 0
        num_registers = 2
        
        # Pack the request
        request = struct.pack('>HHHBBHH', 
                             transaction_id, protocol_id, length, 
                             unit_id, function_code, start_address, num_registers)
        
        print(f"Sending request: {request.hex()}")
        
        # Send request and measure response time
        start_time = time.time()
        sock.send(request)
        
        # Wait for response with timeout
        sock.settimeout(3.0)
        response = sock.recv(1024)
        response_time = time.time() - start_time
        
        print(f"Response received in {response_time:.3f}s: {response.hex()}")
        
        if len(response) >= 9:  # Minimum expected response length
            # Parse response header
            resp_trans_id, resp_proto_id, resp_length, resp_unit_id, resp_func_code = struct.unpack('>HHHBB', response[:8])
            print(f"Response parsed - Trans ID: {resp_trans_id}, Unit: {resp_unit_id}, Function: {resp_func_code}")
            
            if resp_func_code == function_code:
                byte_count = response[8]
                print(f"Data byte count: {byte_count}")
                if len(response) >= 9 + byte_count:
                    data = response[9:9+byte_count]
                    print(f"Register data: {data.hex()}")
                    # Parse as 16-bit registers
                    registers = []
                    for i in range(0, len(data), 2):
                        reg_value = struct.unpack('>H', data[i:i+2])[0]
                        registers.append(reg_value)
                    print(f"Register values: {registers}")
                    return True
            else:
                print(f"Error response - Function code: 0x{resp_func_code:02x}")
        
        sock.close()
        return False
        
    except socket.timeout:
        print("Socket timeout - no response received")
        return False
    except Exception as e:
        print(f"Error: {e}")
        return False

if __name__ == "__main__":
    success = simple_modbus_test("localhost", 5502)
    print(f"Test {'PASSED' if success else 'FAILED'}")