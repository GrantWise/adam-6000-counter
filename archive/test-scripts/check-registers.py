#!/usr/bin/env python3

import socket
import struct
import time

def read_modbus_registers(host, port, unit_id, start_reg, count):
    """Read holding registers via Modbus TCP"""
    try:
        # Create Modbus TCP request
        transaction_id = 1
        protocol_id = 0
        length = 6
        function_code = 3  # Read Holding Registers
        
        # Build request packet
        request = struct.pack('>HHHBBHH', 
                            transaction_id, protocol_id, length, 
                            unit_id, function_code, start_reg, count)
        
        # Send request
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(3.0)
        sock.connect((host, port))
        sock.send(request)
        
        # Read response
        response = sock.recv(1024)
        sock.close()
        
        # Parse response
        if len(response) < 9:
            return None
            
        # Extract register values (skip header)
        reg_data = response[9:]  # Skip 7-byte header + 2-byte byte count
        values = []
        for i in range(0, len(reg_data), 2):
            if i + 1 < len(reg_data):
                val = struct.unpack('>H', reg_data[i:i+2])[0]
                values.append(val)
        
        return values
        
    except Exception as e:
        print(f"Error reading {host}:{port} - {e}")
        return None

def combine_32bit_counter(high_reg, low_reg):
    """Combine two 16-bit registers into 32-bit counter"""
    return (high_reg << 16) | low_reg

print("Checking simulator register values...")
print("=" * 50)

simulators = [
    ("localhost", 5502, "SIM-6051-01"),
    ("localhost", 5503, "SIM-6051-02"), 
    ("localhost", 5504, "SIM-6051-03")
]

for host, port, device_id in simulators:
    print(f"\n{device_id} ({host}:{port}):")
    
    # Read first 4 registers (2 counters × 2 registers each)
    regs = read_modbus_registers(host, port, 1, 0, 4)
    
    if regs:
        print(f"  Raw registers: {regs}")
        
        # Channel 0 (registers 0-1)
        if len(regs) >= 2:
            counter0 = combine_32bit_counter(regs[1], regs[0])  # High, Low
            print(f"  Channel 0 (MainProductCounter): {counter0}")
        
        # Channel 1 (registers 2-3)
        if len(regs) >= 4:
            counter1 = combine_32bit_counter(regs[3], regs[2])  # High, Low
            print(f"  Channel 1 (RejectCounter): {counter1}")
    else:
        print("  Failed to read registers")

print("\nWaiting 5 seconds and checking again for changes...")
time.sleep(5)

print("\n" + "=" * 50)
print("Second reading (5 seconds later):")

for host, port, device_id in simulators:
    print(f"\n{device_id} ({host}:{port}):")
    
    regs = read_modbus_registers(host, port, 1, 0, 4)
    
    if regs:
        print(f"  Raw registers: {regs}")
        
        if len(regs) >= 2:
            counter0 = combine_32bit_counter(regs[1], regs[0])
            print(f"  Channel 0 (MainProductCounter): {counter0}")
        
        if len(regs) >= 4:
            counter1 = combine_32bit_counter(regs[3], regs[2])
            print(f"  Channel 1 (RejectCounter): {counter1}")
    else:
        print("  Failed to read registers")