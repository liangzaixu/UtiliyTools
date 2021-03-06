﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger.Core
{
    public class TextFileReader
    {
        bool _readStart = false;
        bool _readEnd = false;
        System.IO.FileStream _stream = null;
        System.Text.Encoding _code = null;
        long _fileLength = 0;
        long _currentPosition = 0;
        string _readStr = string.Empty;
        int _readBytes = 1024;
        string _filePath = "";
        readonly string[] _defaultOffsetStrArray = new string[] { System.Environment.NewLine, " ", ",", ".", "!", "?", ";", "，", "。", "！", "？", "；" };
        string[] _offsetStrArray = null;

        public string ReadStr
        {
            get { return _readStr; }
        }
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }
        public int ReadBytes
        {
            get { return _readBytes < 1024 ? 1024 : _readBytes; }
            set { _readBytes = value; }
        }
        public string[] OffsetStrArray
        {
            get { return (_offsetStrArray == null || _offsetStrArray.Length < 1) ? _defaultOffsetStrArray : _offsetStrArray; }
            set { _offsetStrArray = value; }
        }


        public TextFileReader()
        {
            _offsetStrArray = _defaultOffsetStrArray;
        }
        public TextFileReader(string FilePath)
        {
            this.FilePath = FilePath;
            _offsetStrArray = _defaultOffsetStrArray;
        }
        private int GetPosition(string readStr, string[] offsetStrArray)
        {
            int position = -1;
            for (int i = 0; i < offsetStrArray.Length; i++)
            {
                position = readStr.LastIndexOf(offsetStrArray[i]);
                if (position > 0)
                {
                    break;
                }
            }
            return position;
        }
        public bool Read()
        {
            if (!_readStart)
            {
                //System.IO.FileShare.ReadWrite：允许其它程序读写文件(重要，否则很可能会与负责写入的程序冲突而被拒绝访问)
                _stream = new System.IO.FileStream(this.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                _code = GetType(this.FilePath);
                _currentPosition = 0;
                _fileLength = _stream.Length;
                _readStart = true;
            }
            if (_currentPosition < _fileLength)
            {
                byte[] readBuffer = new byte[this.ReadBytes];
                //设置读取位置
                _stream.Seek(_currentPosition, System.IO.SeekOrigin.Begin);
                //本次实际读到的字节数
                int currentReadBytes = _stream.Read(readBuffer, 0, readBuffer.Length);
                //读取位置偏移
                _currentPosition += currentReadBytes;

                //实际读到的字节少于指定的字节数(在读到最后一批时)
                if (currentReadBytes < _readBytes)
                {
                    byte[] temp = new byte[currentReadBytes];
                    int index = 0;
                    while (index < currentReadBytes)
                    {
                        temp[index] = readBuffer[index];
                        index++;
                    }
                    readBuffer = temp;
                }
                _readStr = _code.GetString(readBuffer);
                //如果没有读到最后一个字节则计算位置偏移
                if (_currentPosition < _fileLength)
                {
                    int offsetStrPosition = GetPosition(_readStr, this.OffsetStrArray);
                    if (offsetStrPosition > 0)//找到内容则计算位置偏移
                    {
                        //提取将被移除的内容
                        string removeStr = _readStr.Substring(offsetStrPosition + 1);
                        //移除内容
                        _readStr = _readStr.Remove(offsetStrPosition + 1);
                        //位置后退
                        _currentPosition = _currentPosition - _code.GetBytes(removeStr).Length;
                    }
                }
            }
            else
            {
                _readEnd = true;
                _stream.Dispose();
            }
            return !_readEnd;
        }


        public static System.Text.Encoding GetType(string fullname)
        {
            System.IO.FileStream fs = new System.IO.FileStream(fullname, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            System.Text.Encoding r = GetType(fs);
            fs.Close();
            return r;
        }
        public static System.Text.Encoding GetType(System.IO.FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF };
            System.Text.Encoding reVal = System.Text.Encoding.Default;

            System.IO.BinaryReader r = new System.IO.BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = System.Text.Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = System.Text.Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = System.Text.Encoding.Unicode;
            }
            r.Close();
            return reVal;
        }
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;
            byte curByte;
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }


    }
}

