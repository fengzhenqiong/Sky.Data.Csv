# Sky.Data.Csv
A simple and fast CSV reader and writer.



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

The Create static method from both ```CsvReader``` and ```CsvWriter``` can accept a second parameter specifying some options like buffer size, encoding, whether or not to overwrite the existing file when writing a CSV file and whether or not to use cache when reading a CSV file, etc.

The ```UseCache``` option in the ```CsvReaderSettings``` class is really useful when most of the rows in the CSV file are duplicate. This option helps a lot to improve the performance.

Also both ```CsvReaderSettings``` and ```CsvWriterSettings``` support a ```Separator``` option that you can specify the Cell Separator Character when reading or writing CSV files.



## Target

This is a simple but fast implementation of **CsvReader** and **CsvWriter**.

This CsvReader supports all four formats saved by the newest version of Excel, that's **comma seperated**, **ms dos**, **macintosh** and **comma seperated UTF8**. And you can also specify a Cell Separator Character.

If the CSV file is not saved in UTF8 format, you can specify the Encoding in the reader settings.



## License

MIT Licensed.