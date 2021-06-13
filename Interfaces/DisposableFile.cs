using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace MelonLoader.Interfaces
{
    internal class DisposableFile : IDisposable
    {
        private static List<DisposableFile> DisposableFiles = new List<DisposableFile>();
        internal string FilePath { get; private set; } = null;
        internal bool IsDisposed { get; private set; } = false;
        internal bool ShouldDisposeFileData { get; set; } = true;

        internal DisposableFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new ArgumentNullException("filepath");
            FilePath = filepath;
            DisposableFiles.Add(this);
        }

        ~DisposableFile() => InternalDispose();
        public void Dispose() { GC.SuppressFinalize(this); InternalDispose(); }
        private void InternalDispose()
        {
            if (IsDisposed)
                return;
            if (ShouldDisposeFileData)
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            DisposableFiles.Remove(this);
            IsDisposed = true;
        }

        internal static void Cleanup()
        {
            if (DisposableFiles.Count <= 0)
                return;
            DisposableFileEnumerator disposableFileEnumerator = new DisposableFileEnumerator();
            while (disposableFileEnumerator.MoveNext())
                if (!disposableFileEnumerator.Current.IsDisposed)
                    disposableFileEnumerator.Current.Dispose();
        }

        internal bool Download(string file_url, Action<int> callback)
        {
            if (string.IsNullOrEmpty(file_url))
                throw new ArgumentNullException("file_url");
            return WebRequest.DownloadFile(file_url, FilePath, callback);
        }

        internal bool SHA512HashCheckFromURL(string sha512_url)
        {
            if (string.IsNullOrEmpty(sha512_url))
                throw new ArgumentNullException("sha512_url");
            string sha512hash = WebRequest.DownloadString(sha512_url);
            if (string.IsNullOrEmpty(sha512hash))
                return false;
            return SHA512HashCheck(sha512hash);
        }

        internal bool SHA512HashCheck(string sha512hash)
        {
            if (string.IsNullOrEmpty(sha512hash))
                throw new ArgumentNullException("sha512hash");
            SHA512Managed sha512 = new SHA512Managed();
            byte[] checksum = sha512.ComputeHash(File.ReadAllBytes(FilePath));
            if ((checksum == null)
                || (checksum.Length <= 0))
                return false;
            string file_hash = BitConverter.ToString(checksum).Replace("-", string.Empty);
            return (!string.IsNullOrEmpty(file_hash) && file_hash.Equals(sha512hash));
        }

        private class DisposableFileEnumerator : IEnumerator<DisposableFile>
        {
            private int currentIndex = -1;
            public bool MoveNext()
            {
                if ((DisposableFiles.Count <= 0) || (++currentIndex >= DisposableFiles.Count))
                    return false;
                Current = DisposableFiles[currentIndex];
                return true;
            }
            public void Reset() => currentIndex = -1;
            public DisposableFile Current { get; private set; } = null;
            object IEnumerator.Current => Current;
            void IDisposable.Dispose() { }
        }
    }
}