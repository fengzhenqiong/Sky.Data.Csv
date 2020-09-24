# Sky.Data.Csv
A simple and fast comma based CSV reader and writer.



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

The Create static method from both ```CsvReader``` and ```CsvWriter``` can accept a second parameter specifying some options like buffer size, encoding, whether or not to overwrite the existing file or whether or not to use cache when reading a CSV file, etc.

The ```UseCache``` option in the ```CsvReaderSettings``` class is really useful when there are many duplicate rows in the CSV file. This option helps a lot to improve the performance.



## Target

This is a simple implementation of **CsvReader** and **CsvWriter**. It only deals with the very common scenario that assumes the CSV files are saved with Excel or written in same format.

This CsvReader supports all four formats saved by the newest version of Excel, that's **comma seperated**, **ms dos**, **macintosh** and **comma seperated UTF8**.

If the CSV file is not saved in UTF8 format, you can specify the Encoding in the reader settings.



## License

MIT Licensed.