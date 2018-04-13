using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.Xml.Serialization;

namespace WorkerSweetDreams
{
    class Program
    {       
        public static List<WorkerUser> listOfUsers=new List<WorkerUser>();
        public static readonly string pathToUsers = "WorkerUsers.xml";
        public static WorkerUser currentUser; 

        static void Main(string[] args)
        {
            listOfUsers = GetUsers();
                                 
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "q1", durable: false,
                  exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(0, 1, false);
                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: "q1",
                  autoAck: false, consumer: consumer);
                Console.WriteLine(" [x] Awaiting requests");

                consumer.Received += (model, ea) =>
                {
                    string response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body);
                        string[] userData = message.Split(' ');

                        Console.WriteLine($" Recived request from User Id {userData[0]}");
                        

                        for(int i = 0; i < listOfUsers.Count;i++)
                        {
                            if (listOfUsers[i].UserID == Int32.Parse(userData[0]))
                            {
                                response = "You ve alredy sent request. Wait!!!";
                                goto somePoint;
                            }
                        }

                        AddUser(userData);


                        if (listOfUsers.Count >= 3)
                        {
                            response = MatchPair(currentUser).ToString();
                        }
                        else
                        {
                            response = "Our base of people is too small";
                        }
                        somePoint: ;
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" [.] " + e.Message);
                        response = "something has gone wrong ";
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                          basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag,
                          multiple: false);                      
                    }
                };

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
        public static bool CreateUser(WorkerUser addUser)
        {
            List<WorkerUser> Users = GetUsers();
            Users.Add(addUser);

            XmlSerializer formatter = new XmlSerializer(typeof(List<WorkerUser>));
            try
            {
                using (FileStream fs = new FileStream(pathToUsers, FileMode.OpenOrCreate))
                {
                    formatter.Serialize(fs, Users);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static List<WorkerUser> GetUsers()
        {
            List<WorkerUser> Users = new List<WorkerUser>();
            XmlSerializer formatter = new XmlSerializer(typeof(List<WorkerUser>));
            FileInfo fi = new FileInfo(pathToUsers);
            if (fi.Exists)
            {
                using (FileStream fs = new FileStream(pathToUsers, FileMode.OpenOrCreate))
                {
                    Users = (List<WorkerUser>)formatter.Deserialize(fs);
                }
            }
            return Users == null ? new List<WorkerUser>() : Users;
        }
        public static void AddUser(string[] user)
        {
            WorkerUser newUser = new WorkerUser();

            newUser.UserID=Int32.Parse(user[0]);
            newUser.PersonalKey = new Guid(user[1]);
            newUser.Gender= (user[2] == "Male") ?  gender.Male :  gender.Female;
            newUser.LookingFor = (user[3] == "Male") ? gender.Male : gender.Female;

            listOfUsers.Add(newUser);
            CreateUser(newUser);
            currentUser = newUser;                                          
        }

        public static int MatchPair(WorkerUser user)
        {
            int id = 0;
            foreach (var u in listOfUsers)
            {
                if (user.UserID != u.UserID && user.LookingFor == u.Gender)
                {
                    id = u.UserID;
                    listOfUsers.Remove(user);
                    listOfUsers.Remove(u);
                    break;
                }
            }

            File.Delete(pathToUsers);
            

            XmlSerializer formatter = new XmlSerializer(typeof(List<WorkerUser>));
            try
            {
                using (FileStream fs = new FileStream(pathToUsers, FileMode.OpenOrCreate))
                {
                    formatter.Serialize(fs, listOfUsers);
                }
               
            }
            catch (Exception)
            {
            }

            return id;
        }
    }
}
