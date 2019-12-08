use namespace stdio.random;

func Add(a: s32, b: s32) -> s32 
{
    ret a + b;
}

use namespace stdio;

#export
func Test() -> s32 
{
    ret libc.stdio.random.Add(123, 321);
}

#external
#export
func printf(format: u8*, ...);

#export
#external
struct FILE;

#external
#export
func fopen(filename: u8*, mode: u8*) -> FILE*;

#external
#export
func fwrite(ptr: u8*, size: u64, count: u64, file: FILE*) -> u64;

#external
#export
func fclose(file: FILE*) -> s32;

#external
#export
// Should not be here
func strlen(str: u8*) -> u64;