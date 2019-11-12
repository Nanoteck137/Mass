; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@ta = global [2 x [2 x i32]] [[2 x i32] [i32 1, i32 2], [2 x i32] [i32 3, i32 4]]
@str = private unnamed_addr constant [9 x i8] c"test.txt\00", align 1
@str.1 = private unnamed_addr constant [3 x i8] c"wt\00", align 1
@str.2 = private unnamed_addr constant [23 x i8] c"Array: %d, %d, %d, %d\0A\00", align 1
@str.3 = private unnamed_addr constant [11 x i8] c"Value: %d\0A\00", align 1
@str.4 = private unnamed_addr constant [36 x i8] c"ta Value: %d Address: %p Deref: %d\0A\00", align 1
@str.5 = private unnamed_addr constant [10 x i8] c"Test: %s\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i32 @fclose(%struct.FILE*)

define i32 @main(i32 %argc, i8** %argv) {
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %file = alloca %struct.FILE*
  %1 = call %struct.FILE* @fopen(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @str, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @str.1, i32 0, i32 0))
  %2 = load %struct.FILE*, %struct.FILE** %file
  %3 = call i32 @fclose(%struct.FILE* %2)
  %4 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i32 0, i32 0)
  %5 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i32 0, i32 1)
  %6 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i64 1, i32 0)
  %7 = load [2 x i32], [2 x i32]* getelementptr ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i64 1, i32 1)
  %8 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([23 x i8], [23 x i8]* @str.2, i32 0, i32 0), [2 x i32] %4, [2 x i32] %5, [2 x i32] %6, [2 x i32] %7)
  %9 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i32 0, i32 1)
  %10 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @str.3, i32 0, i32 0), [2 x i32] %9)
  %ta = alloca i32
  store i32 11, i32* %ta
  %ptr = alloca i32*
  store i32* %ta, i32** %ptr
  %11 = load i32, i32* %ta
  %12 = load i32*, i32** %ptr
  %13 = load i32*, i32** %ptr
  %14 = load i32, i32* %13
  %15 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([36 x i8], [36 x i8]* @str.4, i32 0, i32 0), i32 %11, i32* %12, i32 %14)
  %16 = load i8**, i8*** %argv2
  %17 = load i8*, i8** %16
  %18 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([10 x i8], [10 x i8]* @str.5, i32 0, i32 0), i8* %17)
  ret i32 0
}
