; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [7 x i8] c"X: %u\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %x = alloca i32
  store i32 4, i32* %x
  br label %then

then:                                             ; preds = %dowhile, %entry
  %0 = load i32, i32* %x
  %1 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str, i32 0, i32 0), i32 %0)
  %2 = load i32, i32* %x
  %3 = sub i32 %2, 1
  store i32 %3, i32* %x
  br label %dowhile

enddowhile:                                       ; preds = %dowhile
  ret i32 0

dowhile:                                          ; preds = %then
  %4 = load i32, i32* %x
  %5 = icmp ugt i32 %4, 0
  br i1 %5, label %then, label %enddowhile
}
