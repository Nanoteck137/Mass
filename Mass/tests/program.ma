use libc.stdio;

func main(args: s32, argv: u8**) -> s32
{
    printf("Enter the number of terms: ");

    var x: s32;
    scanf("%d", addr(x));

    var num: u64 = 0;
    var num2: u64 = 1;
    var next: u64 = 1;
    
    for(var i: s32 = 1; i <= x; i++) {
        printf("%llu, ", num);

        next = num + num2;
        num = num2;
        num2 = next;
    }

    ret 0;
}