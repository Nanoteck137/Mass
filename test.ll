; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [13 x i8] c"Hello World\0A\00", align 1
@str.1 = private unnamed_addr constant [7 x i8] c"A: %u\0A\00", align 1

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
  %a = alloca i32
  store i32 4, i32* %a
  br label %while

then3:                                            ; preds = %then
  %0 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([13 x i8], [13 x i8]* @str, i32 0, i32 0))
  %1 = load i32, i32* %a
  %2 = sub i32 %1, 1
  store i32 %2, i32* %a
  br label %while

endif:                                            ; preds = %then
  %3 = load i32, i32* %a
  %4 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str.1, i32 0, i32 0), i32 %3)
  %5 = load i32, i32* %a
  %6 = sub i32 %5, 1
  store i32 %6, i32* %a
  br label %while

while:                                            ; preds = %endif, %then3, %entry
  %7 = load i32, i32* %a
  %8 = icmp ugt i32 %7, 0
  br i1 %8, label %then, label %endwhile

then:                                             ; preds = %while
  %9 = load i32, i32* %a
  %10 = icmp eq i32 %9, 2
  br i1 %10, label %then3, label %endif

endwhile:                                         ; preds = %while
  ret i32 0
}
