#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <sys/ioctl.h>
#include <sys/time.h>
#include <net/if.h>
#include <linux/can/raw.h>
#include <time.h>

#include "crc16dnp.h"
#include "CanStorage.h"

FILE* output;

void store_frame(struct can_frame msg, uint64_t timestamp) {
        int busnum = 0;
        int dlc = msg.len8_dlc & 0xF;
        int extra_bytes = 8 - msg.len;
        printf("eb: %d\n", extra_bytes);
        if ((uint32_t)msg.can_id & 0x1FFFF800) {
                CST_CANExtID_t canstorage = {
                        CST_HEADER_START_PAT |
                        busnum << CST_HEADER_BUS_Pos | 
                        1 << CST_HEADER_EXID_Pos, 
                        dlc,
                        timestamp,
                        msg.can_id,
                        { 
                                msg.data[0], 
                                msg.data[1], 
                                msg.data[2], 
                                msg.data[3], 
                                msg.data[4], 
                                msg.data[5], 
                                msg.data[6], 
                                msg.data[7], 
                        },
                };
                fwrite(&canstorage, sizeof(CST_CANExtID_t) - extra_bytes, 1, output);

                uint16_t crc = crc16dnp_byte(0, &canstorage, sizeof(CST_CANExtID_t) - extra_bytes);
                fwrite(&crc, sizeof(uint16_t), 1, output);
        } else {
                CST_CANStdID_t canstorage = {
                        CST_HEADER_START_PAT |
                        busnum << CST_HEADER_BUS_Pos | 
                        0 << CST_HEADER_EXID_Pos,
                        dlc,
                        timestamp,
                        msg.can_id,
                        {
                                msg.data[0], 
                                msg.data[1], 
                                msg.data[2], 
                                msg.data[3], 
                                msg.data[4], 
                                msg.data[5], 
                                msg.data[6], 
                                msg.data[7], 
                        },
                };
                fwrite(&canstorage, sizeof(CST_CANStdID_t) - extra_bytes, 1, output);
                
                uint16_t crc = crc16dnp_byte(0, &canstorage, sizeof(CST_CANStdID_t) - extra_bytes);
                fwrite(&crc, sizeof(uint16_t), 1, output);
        }
}

void startlogging() {
        if (output != NULL) 
                return;
        
        time_t current_time = time(NULL);
        struct tm *t = localtime(&current_time);

        char filename[128];
        sprintf(filename, "logs/%d-%d-%d_%02d%02d.log", 
                t->tm_mon + 1, 
                t->tm_mday, 
                t->tm_year + 1900, 
                t->tm_hour, 
                t->tm_min);
        output = fopen(filename, "wb");
}

struct can_frame genFrame(int id, uint64_t data, int dlc) {
        struct can_frame frame;
        frame.len8_dlc = dlc;
        frame.len = dlc;
        frame.can_id = id;
        memcpy(&frame.data, &data, dlc);
        return frame;
}

uint64_t getTimestamp() {
        struct timeval tv;
        gettimeofday(&tv, NULL);

        return 1000000 * tv.tv_sec + tv.tv_usec;
}

int main (void) {
        startlogging();

        store_frame(genFrame(1, 0xFF800000FFFFFF00, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);

        store_frame(genFrame(2, 0xFFFFFFFFFFFFFFFF, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);
        
        store_frame(genFrame(2, 0x0FEEEEEEEEEEEEEE, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);
        
        store_frame(genFrame(2, 0xFFFFFFFFFFFFFFFC, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);
        
        store_frame(genFrame(2, 0xFFFFFFFFFFFFFFFE, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);

        store_frame(genFrame(1, 0xFF800000FFFFFF01, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);
        
        store_frame(genFrame(2, 0xFFFFFFFFFFFFFFFC, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);

        store_frame(genFrame(2, 0x0, 8), getTimestamp());
        for(int i = 0; i < 10000; i++);
}