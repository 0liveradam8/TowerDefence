using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

namespace TowerDefence
{
    public class ConnectionHandler
    {
        private TcpClient client;// Allows connections for tcp network services.
        private StreamReader str; // Provides an interface to read from a stream.
        private StreamWriter stw;// Provides an interface to write to a stream.
        private string recieve;// The incoming string recieved from the other user.
        // Multithreading is used to send and recieve data to the other user so that the game is not interupted waiting for data to transfer.
        private Thread listeningThread;// The thread dedicated to listening for the other user.
        private Thread sendingThread;// The thread dedicated to sending to the other user.
        private Thread serverThread;// The thread dedicated to hosting a server for the users to connect to.
        private TcpListener listener;// Used to listen for when the other user attempts to connect.
        readonly Queue<string> recievedStrings = new Queue<string>();// A buffer to hold incoming data from the other user.
        readonly Queue<string> outgoingBuffer = new Queue<string>();// A buffer to hold outgoing data while it is sent.
        private bool sendingThreadOn;// If true, the program will continually attempt to send data from the outgoing buffer.
        public int BufferLength { get { return outgoingBuffer.Count; } }// A property/accessor to provide a getter for the length of the outgoing buffer.
        void ServerThreadTask(int port)// Starts a server and listens for a client joining on the specified port.
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);// Creates the listener to listen for the other user on the specified port.
                listener.Start();// Starts listening.
                client = listener.AcceptTcpClient();// Sets the tcpclient to the other user trying to join.
                str = new StreamReader(client.GetStream());// Sets the streamreader to read the data sent by the client(the other user).
                stw = new StreamWriter(client.GetStream());// Sets the streamwriter to write to the stream of the client.
                listeningThread = new Thread(ListeningObjectTask);// Starts listening for any data sent by the other user.
                listeningThread.Start();
                sendingThread = new Thread(SendFromBufferTask);// Starts attempting to send any data held in the outgoing buffer.
                sendingThread.Start();
            }
            catch
            {
                // Suppresses the exception that is thrown when _listener.Stop() is called when disconnecting.
            }
        }
        void SendFromBufferTask()// Sends any data that enters the buffer until the user's disconnect from each other.
        {
            sendingThreadOn = true;// Provides a way of escaping the while loop when disconnecting.
            while (sendingThreadOn)
            {
                if (client != null && client.Connected)// If there is a client, and it is connected.
                {
                    lock (outgoingBuffer)// Locks the outgoing buffer from other threads, so that an exception is not thrown.
                    {
                        if (outgoingBuffer.Count > 0)// If there are items in the outgoing buffer waiting to be sent.
                        {
                            stw.WriteLine(outgoingBuffer.Dequeue());// Send the item in the buffer and remove it from the buffer.
                            stw.Flush();// Flush the streamwriter so that it does not throw an exception when it holds too much data.
                        }
                        else
                        {
                            Thread.Sleep(0);// This thread does not need to run any longer if there are no objects waiting to be sent, as it would reduce the performance of
                            // the user's device.
                        }
                    }
                }
            }
            sendingThread.Abort();// If the sending thread is no longer on, abort the thread, and remove the reference to it.
            sendingThread = null;
        }
        void ListeningObjectTask()// Recieves any data sent by the other user.
        {
            while (client != null && client.Connected)// If there is another user and they are connected.
            {
                try// This suppresses exceptions thrown by connection problems, such as the user losing network signal.
                {
                    recieve = str.ReadLine();// Gets the data sent by the other user.
                    if (recieve != "")// If the user has sent data.
                    {
                        lock (recievedStrings)// Locks the buffer to add the data sent by the other user to it.
                        {
                            recievedStrings.Enqueue(recieve);
                        }
                        recieve = "";// Clears the incoming data, as it has been stored.
                    }
                }
                catch (Exception Ex)
                {
                    // Ends all the other multiplayer objects as the connection has ended and they are no longer needed.
                    Disconnect();
                    // Tells the user why they have lost connection, so that they can try to fix the problem.
                    MessageBox.Show("Network connection ended\n" + Ex);
                }
            }
        }
        public void StartServer(int port)// Starts the server thread to listen for the other user. Only needed on the host machine.
        {
            // Creates a thread to run the server task and listen for the other user.
            serverThread = new Thread(() => ServerThreadTask(port));
            serverThread.Start();
        }
        public bool ConnectToServer(IPAddress ip, int port)// Attempts to connect the user to the specified server, using the specified port.
        {
            client = new TcpClient();// Creates a new client to provide the connection.
            IPEndPoint ipEnd = new IPEndPoint(ip, port);// Creates the instance to hold the ip and port.
            try
            {
                client.Connect(ipEnd);// Connect to the machine at the specified ip address using the specified port.
                if (client.Connected)
                {
                    SendObject("Connected");// Send connected to the other user so that they are aware of the connection.
                    MessageBox.Show("Connected to server");// Notifies the user that they are connected to the server, so playing can commence.
                    stw = new StreamWriter(client.GetStream());// Sets the streamwriter to write to the client's stream, so the other user can read it.
                    str = new StreamReader(client.GetStream());// Sets the streamreader to read from the client's stream, to recieve data written by the other user.
                    stw.AutoFlush = true;// Sets the stream writer to flush automatically, so that an exception is not thrown from the stream containing too much data.
                    listeningThread = new Thread(ListeningObjectTask);// Creates and starts the listening thread to listen for data sent by the other user.
                    listeningThread.Start();
                    sendingThread = new Thread(SendFromBufferTask);// Creates and starts the sending thread to send any data added to the sending buffer to the other user.
                    sendingThread.Start();
                    return true;// Returns true to notify the caller than the connection was successful.
                }
            }
            catch (Exception Ex)
            {
                // If the machine the user is attempting to connect to is not accepting clients(if it has not started a server
                // or have not allowed the game through their firewall).
                MessageBox.Show("Unable to connect\n" + Ex.Message);
                
            }
            return false;// The user could not connect to the server.
        }
        public void SendObject<T>(T objectToSend)// Serialises and holds the specified data to send to the other user using the sending thread.
        {
            BinaryFormatter formatter = new BinaryFormatter();// Creates a new formatter to format the specified objects as a string, as a string is needed to write to the stream.
            using (MemoryStream ms = new MemoryStream())// The using statement ensures the memorystream is properly disposed of. This statement creates a new memorystream to hold
            // the serialised object.
            {
                formatter.Serialize(ms, objectToSend);// Serialises the specified object to the memory stream using the binary formatter.
                string base64 = Convert.ToBase64String(ms.ToArray());// Converts the string to a base 64 string, so that the reciever knows the format and can deserialise it.
                lock (outgoingBuffer)// Locks the outgoing buffer so an exception is not thrown by multiple threads attempted to access it.
                {
                    outgoingBuffer.Enqueue(base64);// Adds the serialised object to the buffer, so that the sending thread can send it to the other user.
                }
            }
        }
        public object GetRecievedObject()// Gets the oldest recieved object from the outgoing buffer.
        {
            try
            {
                if (recievedStrings.Count > 0)// If there are serialised objects recieved from the other user.
                {
                    lock (recievedStrings)// Lock the recieved serialised objects, so that they can be changed.
                    {
                        byte[] bytes = Convert.FromBase64String(recievedStrings.Dequeue());// Converts the oldest recieved serialised object to an array of bytes that can be
                        // deserialised.
                        using (MemoryStream ms = new MemoryStream(bytes))// Using a memory stream to hold the byte array.
                        {
                            return new BinaryFormatter().Deserialize(ms);// Deserialise the object and return it, as it is now in a usable format.
                        }
                    }
                }
            }
            catch
            {
                // This suppresses any connection errors and ensures that all the multiplayer objects are properly disposed of.
                Disconnect();
                return null;
            }
            return null;
        }
        public void Disconnect()// Disposes of multiplayer objects so that memory is not still used after the program exits.
        {
            // Because each object is checked whether it is null before processing it, this procedure can be called as a precaution even if
            // one or more of the objects are null.
            // Nullifying references to the objects allows the garbage collector to free their resources.
            if (listeningThread != null)
            {
                listeningThread.Abort();// Aborts the listening thread, so data sent by the other user is no longer recieved.
                listeningThread = null;
            }
            if (listener != null)
            {
                listener.Stop();// Stops listening for a connection to the other user.
                listener = null;
            }
            if (serverThread != null)
            {
                if (serverThread.IsAlive)// If the serverthread is being used.
                {
                    serverThread.Abort();// Abort the serverthread, so that the other user cannot connect to it.
                }
                serverThread = null;
            }
            if (sendingThread != null)
            {
                sendingThreadOn = false;// Meets the ending criteria for the while loop in the sending thread task, so that the thread will end. It cannot be aborted directly
                // from here due to the while loop.
            }
            if (client != null)
            {
                client.Close();// Close the tcpclient, ending connections.
                client = null;
            }
            if (str != null)
            {
                str.Dispose();// Dispose of the read/write streams, freeing up resources.
                str = null;
            }
            if (stw != null)
            {
                stw.Dispose();
                stw = null;
            }
            Game1.TheGame.Connection = Game1.ConnectionState.NoConnection;// Set the connection state to no connection.
            StartGame.Show = false;// Hides the button on the multiplayer screen that allows the user to start the game, as it cannot be started because there is are no connections
            // to another user.
        }
    }
}
