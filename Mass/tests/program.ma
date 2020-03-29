use libc.stdio;

func CalcStringLength(str: u8*) -> s32 {

    var ptr: u64 = 0;
    var length: s32 = 0;
    while(str[ptr] != 0) {
        length += 1;
    }

    ret length;
}

func main(args: s32, argv: u8**) -> s32
{
    printf("Hello World");
    ret 0;
}