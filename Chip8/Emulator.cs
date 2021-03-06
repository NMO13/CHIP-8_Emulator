﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chip8
{
    class Emulator
    {
        private static readonly byte WIDTH = 64;
        private static readonly byte HEIGHT = 32;

        private readonly Stopwatch stopWatch = Stopwatch.StartNew();
        private ushort opcode;
        private byte[] memory;
        private byte[] registers;
        private byte[] V;
        private ushort[] stack;

        private ushort pc;
        private ushort I;

        private byte[] gfx = new byte[WIDTH * HEIGHT];
        private byte delay_timer;
        private byte sound_timer;

        private ushort sp;

        private bool drawFlag;

        private byte[] keyArr = new byte[16];

        public delegate void DrawGraphics(byte[] gfx);
        public event DrawGraphics DrawGraphicsEvent;

        public volatile bool Stop = false;
        public volatile bool Pause = false;

        public void SetKeys(char key, byte val)
        {
            switch(key)
            {
                case '1': keyArr[0x0] = val; break;
                case '2': keyArr[0x1] = val; break;
                case '3': keyArr[0x2] = val; break;
                case '4': keyArr[0x3] = val; break;
                case 'Q': keyArr[0x4] = val; break;
                case 'W': keyArr[0x5] = val; break;
                case 'E': keyArr[0x6] = val; break;
                case 'R': keyArr[0x7] = val; break;
                case 'A': keyArr[0x8] = val; break;
                case 'S': keyArr[0x9] = val; break;
                case 'D': keyArr[0xa] = val; break;
                case 'F': keyArr[0xb] = val; break;
                case 'Z': keyArr[0xc] = val; break;
                case 'X': keyArr[0xd] = val; break;
                case 'C': keyArr[0xe] = val; break;
                case 'V': keyArr[0xf] = val; break;
            }
        }

        public void Initialize()
        {
            Pause = false;
            Stop = false;

            memory = new byte[4096];
            registers = new byte[16];
            V = new byte[16];
            stack = new ushort[16];

            pc = 0x200;
            opcode = 0;
            I = 0;
            sp = 0;

            for(int i = 0; i < 80; i++)
            {
                memory[i] = Fontset.Values[i];
            }
        }

        public void LoadGame(byte[] data)
        {
            int bufferSize = data.Length;
            for (int i = 0; i < bufferSize; ++i)
                memory[i + 512] = data[i];
        }

        public void EmulateCycle()
        {
            // Fetch opcode
            opcode = (ushort) (memory[pc] << 8 | memory[pc + 1]);

            // Decode opcode
            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode & 0x000F)
                    {
                        case 0x0000: // 0x00E0: Clears the screen        
                            for (int i = 0; i < 2048; ++i)
                                gfx[i] = 0x0;
                            drawFlag = true;
                            pc += 2;
                            break;
                        case 0x000E: // 0x00EE: Returns from subroutine
                            pc = stack[--sp];
                            pc += 2;
                            break;
                        default:
                            Console.WriteLine("Unknown opcode [0x0000]: {0:x}\n", opcode);
                            break;
                    }
                    break;
                case 0x1000:
                    pc = (ushort) (opcode & 0x0FFF);
                    break;
                case 0x2000:
                    stack[sp] = pc;
                    ++sp;
                    pc = (ushort) (opcode & 0x0FFF);
                    break;
                case 0x3000:
                    if (V[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF))
                        pc += 4;
                    else
                        pc += 2;
                    break;
                case 0x4000:
                    if (V[(opcode & 0x0F00) >> 8] != (opcode & 0x00FF))
                        pc += 4;
                    else
                        pc += 2;
                    break;
                case 0x5000:
                    if (V[(opcode & 0x0F00) >> 8] == V[(opcode & 0x00F0) >> 4])
                    {
                        pc += 4;
                    }
                    else
                    {
                        pc += 2;
                    }
                    break;
                case 0x6000:
                    V[(opcode & 0x0F00) >> 8] = (byte) (opcode & 0x00FF);
                    pc += 2;
                    break;
                case 0x7000:
                    V[(opcode & 0x0F00) >> 8] += (byte)(opcode & 0x00FF);
                    pc += 2;
                    break;
                case 0x8000:
                    {
                        switch(opcode & 0x000F)
                        {
                            case 0x0000:
                                V[(opcode & 0x0F00) >> 8] = V[(opcode & 0x00F0) >> 4];
                                pc += 2;
                                break;
                            case 0x0001:
                                V[(opcode & 0x0F00) >> 8] |= V[(opcode & 0x00F0) >> 4];
                                pc += 2;
                                break;
                            case 0x0002:
                                V[(opcode & 0x0F00) >> 8] &= V[(opcode & 0x00F0) >> 4];
                                pc += 2;
                                break;
                            case 0x0003:
                                V[(opcode & 0x0F00) >> 8] ^= V[(opcode & 0x00F0) >> 4];
                                pc += 2;
                                break;
                            case 0x0004:
                                if (V[(opcode & 0x00F0) >> 4] > (0xFF - V[(opcode & 0x0F00) >> 8]))
                                    V[0xF] = 1; //carry
                                else
                                    V[0xF] = 0;
                                V[(opcode & 0x0F00) >> 8] += V[(opcode & 0x00F0) >> 4];
                                pc += 2;
                                break;
                            case 0x0005:
                                if (V[(opcode & 0x00F0) >> 4] > V[(opcode & 0x0F00) >> 8])
                                    V[0xF] = 0; //carry
                                else
                                    V[0xF] = 1;
                                V[(opcode & 0x0F00) >> 8] -= V[(opcode & 0x00F0) >> 4];
                                pc += 2;
                                break;
                            case 0x0006:
                                V[0xF] = (byte) (V[(opcode & 0x0F00) >> 8] & 0x01);
                                V[(opcode & 0x0F00) >> 8] >>= 1;
                                V[(opcode & 0x00F0) >> 4] = V[(opcode & 0x0F00) >> 8];
                                pc += 2;
                                break;
                            case 0x0007:
                                if (V[(opcode & 0x0F00) >> 8] > V[(opcode & 0x00F0) >> 4])
                                    V[0xF] = 0; //carry
                                else
                                    V[0xF] = 1;
                                V[(opcode & 0x0F00) >> 8] = (byte) (V[(opcode & 0x00F0) >> 4] - V[(opcode & 0x0F00) >> 8]);
                                pc += 2;
                                break;
                            case 0x000E:
                                V[0xF] = (byte)(V[(opcode & 0x0F00) >> 8] & 0x80);
                                V[(opcode & 0x0F00) >> 8] <<= 1;
                                V[(opcode & 0x00F0) >> 4] = V[(opcode & 0x0F00) >> 8];
                                pc += 2;
                                break;
                            default:
                                Console.WriteLine("Unknown opcode [0x0000]: {0:x}\n", opcode);
                                break;
                        }
                        break;
                    }
                case 0x9000:
                    if(V[(opcode & 0x0F00) >> 8] != V[(opcode & 0x00F0) >> 4])
                    {
                        pc += 4;
                    }
                    else
                    {
                        pc += 2;
                    }
                    break;
                case 0xA000:
                    I = (ushort)(opcode & 0x0FFF);
                    pc += 2;
                    break;
                case 0xB000:
                    pc = (ushort) (V[0] + (opcode & 0x0FFF));
                    pc += 2;
                    break;
                case 0xC000:
                    V[(opcode & 0x0F00) >> 8] = (byte) ((new Random().Next(0, 256)) & (byte) (opcode & 0x00FF));
                    pc += 2;
                    break;
                case 0xD000:
                    {
                        byte x = (byte) (V[(opcode & 0x0F00) >> 8]);
                        byte y = (byte) (V[(opcode & 0x00F0) >> 4]);
                        byte height = (byte) (opcode & 0x000F);
                        byte pixel;

                        V[0xF] = 0;
                        for (byte yline = 0; yline < height; yline++)
                        {
                            pixel = memory[I + yline];
                            for (byte xline = 0; xline < 8; xline++)
                            {
                                if ((pixel & (0x80 >> xline)) != 0) // if sprite pixel is set
                                {
                                    byte posX = (byte) ((x + xline) % WIDTH);
                                    byte posY = (byte)((y + yline) % HEIGHT);

                                    ushort posPixel = (ushort) (posX + ((posY) * 64));

                                    if (gfx[posPixel] == 1) // if pixel value changes from set to unset
                                        V[0xF] = 1; // set vf register
                                    gfx[posPixel] ^= 1;
                                }
                            }
                        }

                        drawFlag = true;
                        pc += 2;
                    }
                    break;
                case 0xE000:
                    switch (opcode & 0x00FF)
                    {
                        // EX9E: Skips the next instruction 
                        // if the key stored in VX is pressed
                        case 0x009E:
                            if (keyArr[V[(opcode & 0x0F00) >> 8]] != 0)
                                pc += 4;
                            else
                                pc += 2;
                            break;
                        case 0x00A1:
                            if (keyArr[V[(opcode & 0x0F00) >> 8]] == 0)
                                pc += 4;
                            else
                                pc += 2;
                            break;
                        default:
                            Console.WriteLine("Unknown opcode: {0:x}\n", opcode);
                            break;
                    }
                    break;
                case 0xF000:
                    switch (opcode & 0x00FF)
                    {
                        case 0x0007:
                            V[(opcode & 0x0F00) >> 8] = delay_timer;
                            pc += 2;
                            break;
                        case 0x000A:
                            bool keyPress = false;

                            for (byte i = 0; i < 16; ++i)
                            {
                                if (keyArr[i] != 0)
                                {
                                    V[(opcode & 0x0F00) >> 8] = i;
                                    keyPress = true;
                                }
                            }

                            // If we didn't received a keypress, skip this cycle and try again.
                            if (!keyPress)
                                return;

                            pc += 2;
                            break;
                        case 0x0015:
                            delay_timer = V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;
                        case 0x0018:
                            sound_timer = V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;
                        case 0x001E:
                            I += V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;
                        case 0x0033:
                            memory[I] = (byte) (V[(opcode & 0x0F00) >> 8] / 100);
                            memory[I + 1] = (byte) ((V[(opcode & 0x0F00) >> 8] / 10) % 10);
                            memory[I + 2] = (byte) ((V[(opcode & 0x0F00) >> 8] % 100) % 10);
                            pc += 2;
                            break;
                        case 0x0029:
                            I = (byte) (V[(opcode & 0x0F00) >> 8] * 0x5);
                            pc += 2;
                            break;
                        case 0x0055:
                            byte length = (byte)((opcode & 0x0F00) >> 8);
                            for(byte i = 0; i <= length; i++)
                            {
                                memory[I++] = V[i];
                            }
                            pc += 2;
                            break;
                        case 0x0065:
                            length = (byte)((opcode & 0x0F00) >> 8);
                            for(byte i = 0; i <= length; i++)
                            {
                                V[i] = memory[I++];
                            }
                            pc += 2;
                            break;
                        default:
                            Console.WriteLine("Unknown opcode: {0:x}\n", opcode);
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("Unknown opcode: {0:x}\n", opcode);
                    break;
            }

            // Update timers
            if (delay_timer > 0)
            {
                --delay_timer;
                Thread.Sleep(1 / 60);
            }

            if (sound_timer > 0)
            {
                Console.Beep(500, (1000 / 60) * sound_timer);
                sound_timer = 0;
            }
        }

        public void Run()
        {
            TimeSpan timePerFrame = new TimeSpan(TimeSpan.TicksPerSecond / 540); // CHIP8 runs at 540 HZ
            while(!Stop)
            {
                while(Pause) { DrawGraphicsEvent(gfx); }
                var startTime = stopWatch.Elapsed;
              
                EmulateCycle();

                if(drawFlag)
                {
                    DrawGraphicsEvent(gfx);
                    drawFlag = false;
                }

                var elapsedTime = stopWatch.Elapsed - startTime;
                if(elapsedTime < timePerFrame)
                {
                    Thread.Sleep(timePerFrame - elapsedTime);
                }
            }

            Console.WriteLine("Shutting down");
            gfx = new byte[WIDTH * HEIGHT];
            DrawGraphicsEvent(gfx);
        }

    }
}
