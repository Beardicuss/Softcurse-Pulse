from PIL import Image

def convert_to_transparent(input_path, output_path):
    img = Image.open(input_path).convert("RGBA")
    datas = img.getdata()

    new_data = []
    # The attached image is a red heartbeat on a pure white background.
    for item in datas:
        # Check if pixel is white or very light gray
        if item[0] > 230 and item[1] > 230 and item[2] > 230:
            new_data.append((255, 255, 255, 0)) # Transparent
        else:
            new_data.append(item)
            
    img.putdata(new_data)
    img.save(output_path, format="ICO", sizes=[(64, 64)])

convert_to_transparent(r"D:\Dev\Projects\Experiments\Pulse\pulse_logo.png", r"D:\Dev\Projects\Experiments\Pulse\Pulse.App\red_pulse.ico")
