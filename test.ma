#external
struct FILE;

#external
func printf(format: u8*, ...) -> s32;

#external
func fopen(filename: u8*, mode: u8*) -> FILE*;

#external
func fwrite(ptr: u8*, size: u64, count: u64, file: FILE*) -> u64;

#external
func fclose(file: FILE*) -> s32;

#external
func strlen(str: u8*) -> u64;

func main(argc: s32, argv: u8**) -> s32 
{
	var a: f32 = -3.14f;
	var b: s32 = 3;
	var c: f32 = a + b;

	for(var i: s32 = 0; i < 10; i++) 
	{
		//printf("i: %d\n", i);
	}

	printf("C: %f\n", c);

	ret 0;
}
