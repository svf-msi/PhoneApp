using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace StandardLib
{
    public class FftAnalysis
    {
        public static double[,] Emgu(double[] data, WindowType window = WindowType.Uniform, bool padFour = true)
        {
            var span = (padFour && data.Length % 4 > 0) ? data.Length + (4 - data.Length % 4) : data.Length;
            var windowFunction = GetWindowFunction(window, span, out var correction);
            var spectrum = new double[1, span];
            var scale = 1.0 / correction * 4; // extra x4 is for 2x p2p adjustement and another 2x for fft amplitude reduction

            for (int t = 0; t < span; ++t)
            {
                if (t < data.Length)
                    spectrum[0, t] = data[t] * windowFunction[t] * scale;
                else
                    spectrum[0, t] = data[0] * windowFunction[0] * scale;
            }

            GCHandle handle = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(1, span, DepthType.Cv64F, 1, pointer, 8 * span);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Debug]: Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            return spectrum;
        }

        public static double[,] Emgu(byte[] data, WindowType window = WindowType.Uniform, bool padFour = true)
        {
            var span = (padFour && data.Length % 4 > 0) ? data.Length + (4 - data.Length % 4) : data.Length;
            //Console.WriteLine($"check fft length: {data.Length} -> {span}");
            var windowFunction = GetWindowFunction(window, span, out var correction);
            var spectrum = new double[1, span];
            var scale = 1.0 / correction;

            for (int t = 0; t < span; ++t)
            {
                if (t < data.Length)
                    spectrum[0, t] = data[t] * windowFunction[t] * scale;
                else
                    spectrum[0, t] = data[0] * windowFunction[0] * scale;
            }

            GCHandle handle = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(1, span, DepthType.Cv64F, 1, pointer, 8 * span);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            return spectrum;
        }

        public static double[,] Emgu(ushort[] data, WindowType window = WindowType.Uniform, bool padFour = true)
        {
            var span = (padFour && data.Length % 4 > 0) ? data.Length + (4 - data.Length % 4) : data.Length;
            //Console.WriteLine($"check fft length: {data.Length} -> {span}");
            var windowFunction = GetWindowFunction(window, span, out var correction);
            var spectrum = new double[1, span];
            var scale = 1.0 / correction;

            for (int t = 0; t < span; ++t)
            {
                if (t < data.Length)
                    spectrum[0, t] = data[t] * windowFunction[t] * scale;
                else
                    spectrum[0, t] = data[0] * windowFunction[0] * scale;
            }

            GCHandle handle = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(1, span, DepthType.Cv64F, 1, pointer, 8 * span);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            return spectrum;
        }

        public static double[,] Emgu(byte[,] data, WindowType window, bool padFour = true)
        {
            var width = data.GetLength(0);
            var length = data.GetLength(1);
            var span = (padFour && length % 4 > 0) ? length + (4 - length % 4) : length;
            var windowFunction = GetWindowFunction(window, span, out var correction);
            var spectrum = new double[width, span];
            var scale = 1.0 / correction;

            for (int x = 0; x < width; ++x)
            {
                for (int t = 0; t < span; ++t)
                {
                    if (t < length)
                        spectrum[x, t] = data[x, t] * windowFunction[t] * scale;
                    else
                        spectrum[x, t] = data[x, 0] * windowFunction[0] * scale;
                }
            }

            GCHandle handle = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(width, span, DepthType.Cv64F, 1, pointer, 8 * span);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            return spectrum;
        }

        public static double[,] Emgu(ushort[,] data, WindowType window, bool padFour = true)
        {
            var width = data.GetLength(0);
            var length = data.GetLength(1);
            var span = (padFour && length % 4 > 0) ? length + (4 - length % 4) : length;
            var windowFunction = GetWindowFunction(window, span, out var correction);
            var spectrum = new double[width, span];
            var scale = 1.0 / correction;

            for (int x = 0; x < width; ++x)
            {
                for (int t = 0; t < span; ++t)
                {
                    if (t < length)
                        spectrum[x, t] = data[x, t] * windowFunction[t] * scale;
                    else
                        spectrum[x, t] = data[x, 0] * windowFunction[0] * scale;
                }
            }

            GCHandle handle = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(width, span, DepthType.Cv64F, 1, pointer, 8 * span);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            return spectrum;
        }

        public static double[,] EmguRaw(double[] data)
        {
            var span = data.Length;
            var spectrum = new double[1, span];

            Buffer.BlockCopy(data, 0, spectrum, 0, span);
            GCHandle handle = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(1, span, DepthType.Cv64F, 1, pointer, 8 * span);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            return spectrum;
        }

        public static double[,] ForwardDFT(byte[,] data)
        {
            var width = data.GetLength(0);
            var length = data.GetLength(1);
            var spectrum = new double[width, length];

            if (length % 4 != 0)
            {
                Console.WriteLine($"Array length {length} is not multiple of 4! Aborting.");
                return null;
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            GCHandle handle2 = GCHandle.Alloc(spectrum, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(width, length, DepthType.Cv8U, 1, pointer, length);
                var matIn = new Mat();
                mat.ConvertTo(matIn, DepthType.Cv64F);
                IntPtr pointer2 = handle2.AddrOfPinnedObject();
                var matOut = new Mat(width, length, DepthType.Cv64F, 1, pointer2, 8 * length);
                CvInvoke.Dft(matIn, matOut, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
                return null;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
                if (handle2.IsAllocated)
                {
                    handle2.Free();
                }
            }
            return spectrum;
        }

        public static void ForwardDFT(double[,] data)
        {
            var width = data.GetLength(0);
            var length = data.GetLength(1);

            if (length % 2 != 0)
            {
                Console.WriteLine($"Array length {length} is not multiple of 4! Aborting.");
                return;
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(width, length, DepthType.Cv64F, 1, pointer, 8 * length);
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public static void InverseDFT(double[,] data)
        {
            var width = data.GetLength(0);
            var length = data.GetLength(1);

            if (length % 2 != 0)
            {
                Console.WriteLine($"Array length {length} is not an even number! Aborting.");
                return;
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                var mat = new Mat(width, length, DepthType.Cv64F, 1, pointer, 8 * length);
                CvInvoke.Dft(mat, mat, DxtType.Inverse | DxtType.Rows, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        /// <summary>
        /// Requires x4 array length
        /// </summary>
        /// <param name="data"></param>
        public static void FilterWaveformInPlace(byte[] data, double offset, double[,] filter)
        {
            var span = data?.Length ?? 0;
            if (span == 0 || span % 4 != 0 || filter?.GetLength(0) != 1 || span != filter.GetLength(1)) return;
            var processed = new byte[1, span];

            Buffer.BlockCopy(data, 0, processed, 0, span);
            GCHandle handleData = GCHandle.Alloc(processed, GCHandleType.Pinned);
            GCHandle handleFilter = GCHandle.Alloc(filter, GCHandleType.Pinned);
            try
            {
                IntPtr pointerData = handleData.AddrOfPinnedObject();
                var matIn = new Mat(1, span, DepthType.Cv8U, 1, pointerData, span);
                var mat = new Mat();
                matIn.ConvertTo(mat, DepthType.Cv64F);
                IntPtr pointerFilter = handleFilter.AddrOfPinnedObject();
                var matFilter = new Mat(1, span, DepthType.Cv64F, 1, pointerFilter, 8 * span);

                // Subtract reference
                mat = mat - offset; 

                // Forward FFT
                CvInvoke.Dft(mat, mat, DxtType.Forward | DxtType.Scale | DxtType.Rows, 0);

                // Apply filter mask
                CvInvoke.Multiply(mat, matFilter, mat);

                // Inverse FFT
                CvInvoke.Dft(mat, mat, DxtType.Inverse | DxtType.Rows, 0);

                // Add reference
                mat = mat + offset;

                // Trim?

                // Copy back to source
                mat.ConvertTo(matIn, DepthType.Cv8U);
                Buffer.BlockCopy(processed, 0, data, 0, span);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in emgu fft: {e}");
            }
            finally
            {
                if (handleData.IsAllocated)
                {
                    handleData.Free();
                }

                if (handleFilter.IsAllocated)
                {
                    handleFilter.Free();
                }
            }
        }

        public static Complex Complex(double[,] data, int index1, int index2)
        {
            if (index1 < 0 || index1 >= data.GetLength(0)) return 0;
            if (index2 < 0 || index2 >= data.GetLength(1)) return 0;
            if (index2 == data.GetLength(1) - 1)
            {
                return new Complex(data[index1, index2], 0);
            }
            else
            {
                return new Complex(data[index1, index2], data[index1, index2 + 1]);
            }
        }

        public static double[] GetWindowFunction(WindowType window, int length, out double correction)
        {
            switch (window)
            {
                case WindowType.Hann:
                    correction = 0.5;
                    return MathNet.Numerics.Window.Hann(length);
                case WindowType.Hamming:
                    correction = 25.0 / 46.0;
                    return MathNet.Numerics.Window.Hamming(length);
                case WindowType.Nuttall:
                    correction = 0.355768;
                    return MathNet.Numerics.Window.Nuttall(length);
                case WindowType.Blackman:
                    correction = 0.42659;
                    return MathNet.Numerics.Window.Blackman(length);
                case WindowType.BlackmanNuttall:
                    correction = 0.3635819;
                    return MathNet.Numerics.Window.BlackmanNuttall(length);
                case WindowType.BlackmanHarris:
                    correction = 0.35875;
                    return MathNet.Numerics.Window.BlackmanHarris(length);
                case WindowType.FlatTop:
                    correction = 1;
                    return MathNet.Numerics.Window.FlatTop(length);
                case WindowType.Exponential:
                    correction = 1;
                    double width = length / 3;
                    return Enumerable.Range(0,length).Select(v => Math.Exp(-v / width)).ToArray();
                default:
                    correction = 1;
                    return MathNet.Numerics.Window.Dirichlet(length);
            }
        }

    }

    public enum WindowType
    {
        Uniform,
        Hann,
        Hamming,
        Nuttall,
        Blackman,
        BlackmanNuttall,
        BlackmanHarris,
        FlatTop,
        Exponential
    }

}
