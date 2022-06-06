# VHDLLanguageService
I started doing VHDL stuff some time ago, so naturally I looked for a VHDL extension for Visual studio. Unfortunately, the existing extensions are not free. So I decided to make my own extension for VHDL.
There is a bit of vivado integration as well but I didn't have enough time to finish it. And tcl being tcl... makes it rather annoying as well.

There is quite a few bugs. VHDL is not exactly a simple language, but I feel like there is already a lot of usefull features and error checks. The design feels pretty solid. So it's quite easy to fix things and add new features.

Feel free to contribute, even if it's just a small fix.

# Supported features

### Context aware syntax coloring
![image](https://user-images.githubusercontent.com/99695100/170562106-6179298e-eee7-4754-8d9f-2608d36242fa.png)

### Type evaluation
![image](https://user-images.githubusercontent.com/99695100/170529491-b065ba62-2b4a-4488-93b2-162a671f5519.png)

### Functions/array signature help
![image](https://user-images.githubusercontent.com/99695100/170529609-3f8b7c77-8250-4f2e-b129-5d8514972a9d.png)

### Outlining
![image](https://user-images.githubusercontent.com/99695100/170530029-097eb58d-2624-49ae-b855-b32bf69fbd06.png)

### Completion
![image](https://user-images.githubusercontent.com/99695100/170561963-0508ed3e-3cc8-428a-8e05-5288629e9820.png)

### Library path
![image](https://user-images.githubusercontent.com/99695100/170562949-82136cd0-264b-438a-a401-9262d5fc081d.png)

### Function result evaluation
Small functions code can be executed to evaluate the result, this allow to catch more errors.  
*This feature might need to be enabled in Tools -> Options -> Text Editor -> VHDL -> Advanced*  
![image](https://user-images.githubusercontent.com/99695100/172079280-f4141254-9cac-4881-8b2f-26948ce33ded.png)

### Sensitivity list mistakes
![image](https://user-images.githubusercontent.com/99695100/172079659-cc4f9d1c-2472-418e-a794-76d7e0e188ad.png)

