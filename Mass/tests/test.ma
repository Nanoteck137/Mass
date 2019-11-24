#external
func printf(format: u8*, ...);

func main(argc: s32, argv: u8**) -> s32
{
    libc.printf("Hello World\n");
	var result: s32 = libc.add(4, 5);
	printf("Wooh %d\n", result);
    
    ret 0;
}
 