namespace SynchronizationPrimitives
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var synchronizedCache = new SynchronizedCache();
            var tasks = new List<Task>();
            int itemsWritten = 0;

            // Execute a writer.
            tasks.Add(Task.Run(() => {  
                string [] vegetables = 
                { 
                    "broccoli", "cauliflower",
                    "carrot", "sorrel", "baby turnip",
                    "beet", "brussel sprout",
                    "cabbage", "plantain",
                    "spinach", "grape leaves",
                    "lime leaves", "corn",
                    "radish", "cucumber",
                    "raddichio", "lima beans" 
                };

                for (int counter = 1; counter <= vegetables.Length; counter++)
                {
                    synchronizedCache.Add(counter, vegetables[counter - 1]);
                }

                itemsWritten = vegetables.Length;
                Console.WriteLine($"Task {Task.CurrentId} wrote {itemsWritten} items\n");
            }));

            // Execute two readers, one to read from first to last and the second from last to first.
            for (int counter = 0; counter < 2; counter++)
            {
                bool isDescending = counter == 1;
                
                tasks.Add(Task.Run(() => {
                    int start, last, step;
                    int itemsCount;

                    do
                    {
                        string output = "";
                        itemsCount = synchronizedCache.Count;

                        if (!isDescending)
                        {
                            start = 1;
                            step = 1;
                            last = itemsCount;
                        }
                        else
                        {
                            start = itemsCount;
                            step = -1;
                            last = 1;
                        }

                        for (int index = start; isDescending ? index >= last : index <= last; index += step)
                        {
                            output += new string($"[{synchronizedCache.Read(index)}] ");
                        }

                        Console.WriteLine($"Task {Task.CurrentId} read {itemsCount} items: {output}\n");
                    } 
                    while (itemsCount < itemsWritten | itemsWritten == 0);
                }));
            }

            // Execute a write/update task.
            tasks.Add(Task.Run(() => {
                Thread.Sleep(100);

                for (int counter = 1; counter <= synchronizedCache.Count; counter++)
                {
                    string value = synchronizedCache.Read(counter);
                    if (value == "cucumber" && synchronizedCache.AddOrUpdate(counter, "green bean") != SynchronizedCache.AddOrUpdateStatus.Unchanged)
                    {
                        Console.WriteLine("Changed 'cucumber' to 'green bean'");
                    }
                }
            }));

            // Wait for all three tasks to complete.
            Task.WaitAll(tasks.ToArray());

            // Display the final contents of the cache.
            Console.WriteLine();
            Console.WriteLine("Values in synchronized cache: ");
            for (int counter = 1; counter <= synchronizedCache.Count; counter++)
            {
                Console.WriteLine($"{counter,3}: {synchronizedCache.Read(counter)}");
            }
        }
    }
}