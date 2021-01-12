# Sky.Data.Csv
A simple and extremely fast CSV reader and writer which supports reading and writing raw CSV values and typed objects.



## Usage

It's really simple to use. Following is an example:

```C#
using Sky.Data.Csv;

//reader and writer sample
using(var reader = CsvReader.Create("path-to-file")){
    using(var writer = CsvWriter.Create("path-to-file")){
        foreach(var row in reader){
            writer.WriteRow(row);
        }
    }
}
```

The ```Create``` static method from both ```CsvReader``` and ```CsvWriter``` can accept a second parameter specifying some options like buffer size, encoding, whether or not to overwrite the existing file when writing a CSV file and whether or not to use cache when reading a CSV file, etc.

The ```UseCache``` option in the ```CsvReaderSettings``` class is really useful when most of the rows in the CSV file are duplicate. This option helps a lot to improve the performance.

Also both ```CsvReaderSettings``` and ```CsvWriterSettings``` support a ```Separator``` option that you can specify the Cell Separator Character when reading or writing CSV files.



### Support reading and writing objects

If you use the generic version of ```CsvReader<T>``` and ```CsvWriter<T>```, the ```Create``` static method will also accept a parameter of type ```IDataResolver<T>``` which supports ```CsvReader``` and ```CsvWriter``` to read and write typed objects. 

The ```AbstractDataResolver<T>``` provides a base type with some basic implementation and you can create subclasses of it.

```c#
public class Student
{
    public String Name { get; set; }
    public String Address { get; set; }

    public Int32 Height { get; set; }
    public DateTime Birthday { get; set; }
}

public class StudentResolver : AbstractDataResolver<Student>
{
    public override Student Deserialize(List<String> data)
    {
        var culture = CultureInfo.InvariantCulture;
        return new Student
        {
            Name = data[0],
            Height = Int32.Parse(data[1]),
            Birthday = DateTime.ParseExact(data[2], "yyyy-MM-dd", culture),
            Address = data[3],
        };
    }

    public override List<String> Serialize(Student data)
    {
        return new List<String>
        {
            data.Name,
            data.Height.ToString(),
            data.Birthday.ToString("yyyy-MM-dd"),
            data.Address,
        };
    }
}

static void Main(String[] args)
{
    var dataResolver = new StudentResolver();
    var csvPath = "path-to-csv-file";
    using (var reader = CsvReader<Student>.Create(csvPath, dataResolver))
    {
        foreach (var student in reader)
        {
            Console.WriteLine(student.Address);
        }
    }
}
```



## Run tests in Linux

In earlier versions of the source codes, the **test project** only supports Windows OS. Now it also supports Linux.

If you want to run the test codes in Linux (**tested on Ubuntu 18.04 LTS x64**), firstly change the current directory to **Sky.Data.Csv.Test** and then run following commands in the terminal:

```
Sky.Data.Csv.Test$ dotnet run -c Release --project Sky.Data.Csv.Test.Core.csproj
```



## Target

This is a simple but fast implementation of **```CsvReader```** and **```CsvWriter```**.

This CsvReader supports all four formats saved by the newest version of Excel, that's **comma seperated**, **ms dos**, **macintosh** and **comma seperated UTF8**. 

By default, the ```CsvReaderSettings``` and ```CsvWriterSettings``` uses ```Encoding.Default``` as the default encoding and comma (,) as the field separator. You can also specify these settings (**and some other settings, many settings are supported**) by creating a new setting object and pass it to the ```Create``` static method.



## License

MIT Licensed.