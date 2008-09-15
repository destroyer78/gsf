using System.Diagnostics;
using System.Linq;
using System.Data;
using System.Collections;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using TVA.IO.FilePath;
using TVA.Threading;

// James Ritchie Carroll - 2003


namespace TVA
{
    namespace Net
    {
        namespace Ftp
        {


            public enum TransferDirection
            {
                Upload,
                Download
            }

            internal class FileTransferer
            {


                delegate void FileCommandDelegate(string remoteFileName);
                delegate void StreamCopyDelegate(Stream remote, Stream local);

                private StreamCopyDelegate m_streamCopyRoutine;
                private FileCommandDelegate m_ftpFileCommandRoutine;

                private Directory m_transferStarter;
                private SessionConnected m_session;
                private string m_localFile;
                private string m_remoteFile;
                private long m_totalBytes;
                private long m_totalBytesTransfered;
                private int m_transferedPercentage;
                private TransferDirection m_transferDirection;
                private FileMode m_localFileOpenMode;
                private AsyncResult m_transferResult;

                public string LocalFileName
                {
                    get
                    {
                        return m_localFile;
                    }
                }

                public string RemoteFileName
                {
                    get
                    {
                        return m_remoteFile;
                    }
                }

                public long TotalBytes
                {
                    get
                    {
                        return m_totalBytes;
                    }
                }

                public long TotalBytesTransfered
                {
                    get
                    {
                        return m_totalBytesTransfered;
                    }
                }

                public TransferDirection TransferDirection
                {
                    get
                    {
                        return m_transferDirection;
                    }
                }

                public AsyncResult TransferResult
                {
                    get
                    {
                        return m_transferResult;
                    }
                }

                public int TransferedPercentage
                {
                    get
                    {
                        return m_transferedPercentage;
                    }
                }

                internal FileTransferer(Directory transferStarter, string localFile, string remoteFile, long totalBytes, TransferDirection dir)
                {

                    m_transferStarter = transferStarter;
                    m_transferDirection = dir;
                    m_session = transferStarter.Session;
                    m_localFile = localFile;
                    m_remoteFile = remoteFile;
                    m_totalBytes = totalBytes;

                    if (dir == TransferDirection.Upload)
                    {
                        m_streamCopyRoutine = new System.EventHandler(LocalToRemote);
                        m_ftpFileCommandRoutine = new System.EventHandler(m_session.ControlChannel.STOR);
                        m_localFileOpenMode = FileMode.Open;
                    }
                    else
                    {
                        m_streamCopyRoutine = new System.EventHandler(RemoteToLocal);
                        m_ftpFileCommandRoutine = new System.EventHandler(m_session.ControlChannel.RETR);
                        m_localFileOpenMode = FileMode.Create;
                    }

                }

                private void TransferThreadProc()
                {

                    try
                    {
                        StartTransfer();
                        m_session.Host.RaiseFileTranferNotification(new AsyncResult("Success.", AsyncResult.Complete));
                    }
                    catch (Exception e)
                    {
                        m_session.Host.RaiseFileTranferNotification(new AsyncResult("Transfer fail: " + e.Message, AsyncResult.Fail));
                    }

                }

                internal void StartTransfer()
                {

                    FileStream localStream = null;
                    DataStream remoteStream = null;

                    try
                    {
                        // Files just created may still have a file lock, we'll wait a few seconds for read access if needed...
                        if (m_transferDirection == TransferDirection.Upload)
                        {
                            WaitForReadLock(m_localFile, m_session.Host.WaitLockTimeout);
                        }

                        m_session.Host.RaiseBeginFileTransfer(m_localFile, m_remoteFile, m_transferDirection);

                        localStream = new FileStream(m_localFile, m_localFileOpenMode);
                        remoteStream = m_session.ControlChannel.GetPassiveDataStream(m_transferDirection);

                        m_ftpFileCommandRoutine(m_remoteFile);
                        m_streamCopyRoutine(remoteStream, localStream);

                        remoteStream.Close();
                        TestTransferResult();

                        m_session.Host.RaiseEndFileTransfer(m_localFile, m_remoteFile, m_transferDirection, m_transferResult);
                    }
                    catch
                    {
                        if (remoteStream != null)
                        {
                            remoteStream.Close();
                        }
                        if (localStream != null)
                        {
                            localStream.Close();
                        }
                        m_session.Host.RaiseEndFileTransfer(m_localFile, m_remoteFile, m_transferDirection, m_transferResult);
                        throw;
                    }
                    finally
                    {
                        if (remoteStream != null)
                        {
                            remoteStream.Close();
                        }
                        if (localStream != null)
                        {
                            localStream.Close();
                        }
                    }

                }

                internal void StartAsyncTransfer()
				{
					
					#if ThreadTracking
					object with_1 = new ManagedThread(TransferThreadProc);
					with_1.Name = "TVA.Net.Ftp.FileTransferer.TransferThreadProc() [" + m_remoteFile + "]";
					#else
					System.Threading.Thread with_2 = new Thread(new System.Threading.ThreadStart(TransferThreadProc));
					with_2.Name = "Transfer file thread: " + m_remoteFile;
					#endif
					.Start();
					
				}

                private void TestTransferResult()
                {

                    int responseCode = m_session.ControlChannel.LastResponse.Code;

                    if (responseCode == Response.ClosingDataChannel)
                    {
                        return;
                    }

                    if (responseCode == Response.RequestFileActionComplete)
                    {
                        return;
                    }

                    throw (new DataTransferException("Failed to transfer file.", m_session.ControlChannel.LastResponse));

                }

                private void RemoteToLocal(Stream remote, Stream local)
                {

                    StreamCopy(local, remote);

                }

                private void LocalToRemote(Stream remote, Stream local)
                {

                    StreamCopy(remote, local);

                }

                private void StreamCopy(Stream dest, Stream @source)
                {

                    int byteRead;
                    long onePercentage;
                    long bytesReadFromLastProgressEvent;
                    byte[] buffer = new byte[4 * 1024 + 1];

                    onePercentage = m_totalBytes / 100;
                    bytesReadFromLastProgressEvent = 0;
                    byteRead = @source.Read(buffer, 0, 4 * 1024);

                    while (byteRead != 0)
                    {
                        m_totalBytesTransfered += byteRead;
                        bytesReadFromLastProgressEvent += byteRead;

                        if (bytesReadFromLastProgressEvent > onePercentage)
                        {
                            m_transferedPercentage = (int)(((float)m_totalBytesTransfered) / ((float)m_totalBytes) * 100);
                            m_session.Host.RaiseFileTransferProgress(m_totalBytes, m_totalBytesTransfered, m_transferDirection);
                            bytesReadFromLastProgressEvent = 0;
                        }

                        dest.Write(buffer, 0, byteRead);
                        byteRead = @source.Read(buffer, 0, 4 * 1024);
                    }

                }

            }

        }
    }
}
