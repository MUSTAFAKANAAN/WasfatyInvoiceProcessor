# Character Sanitization for Invoice Data

## Overview

The application automatically **sanitizes text fields** in invoice data before sending to the Wasfaty API. This removes special characters that could cause issues with the API or database.

## Where Sanitization Occurs

Sanitization happens in `RemoteDatabaseService.cs` when fetching invoices from the remote database:

1. **Invoice Line Descriptions** - All product descriptions are cleaned
2. **Customer Names** - Customer names are cleaned

## Characters ALLOWED ✅

The following characters are **kept** in the text:

| Character Type | Characters | Example |
|----------------|-----------|---------|
| Uppercase Letters | A-Z | PANADOL |
| Lowercase Letters | a-z | tablet |
| Digits | 0-9 | 500mg |
| Arabic Characters | ء-ي (Unicode 0600-06FF) | بانادول |
| Space | ` ` (space) | "500 mg" |
| Period | `.` | "Dr. Smith" |
| Comma | `,` | "Smith, John" |
| Dash/Hyphen | `-` | "Co-Amoxiclav" |
| Forward Slash | `/` | "500mg/tab" |

## Characters REMOVED ❌

All other characters are **removed**, including the ones you specifically requested:

### Explicitly Removed Special Characters:

| Character | Name | Example |
|-----------|------|---------|
| `(` | Opening Parenthesis | Removed from "Tablet (50mg)" → "Tablet 50mg" |
| `)` | Closing Parenthesis | Removed from "Tablet (50mg)" → "Tablet 50mg" |
| `+` | Plus Sign | Removed from "A+B" → "AB" |
| `=` | Equals Sign | Removed from "2=1" → "21" |
| `&` | Ampersand | Removed from "Smith & Sons" → "Smith Sons" |
| `#` | Hash/Number Sign | Removed from "#123" → "123" |
| `@` | At Sign | Removed from "test@mail" → "testmail" |
| `!` | Exclamation Mark | Removed from "URGENT!" → "URGENT" |

### Other Removed Characters:

| Character | Name |
|-----------|------|
| `:` | Colon |
| `;` | Semicolon |
| `%` | Percent |
| `*` | Asterisk |
| `<` | Less Than |
| `>` | Greater Than |
| `[` | Opening Bracket |
| `]` | Closing Bracket |
| `{` | Opening Brace |
| `}` | Closing Brace |
| `\|` | Pipe/Vertical Bar |
| `\` | Backslash |
| `"` | Double Quote |
| `'` | Single Quote |
| `` ` `` | Backtick |
| `~` | Tilde |
| `?` | Question Mark |
| `$` | Dollar Sign |
| `^` | Caret |
| `_` | Underscore |

## Examples

### Product Descriptions

| Original | Sanitized |
|----------|-----------|
| `Panadol (500mg)` | `Panadol 500mg` |
| `Co-Amoxiclav 625mg` | `Co-Amoxiclav 625mg` ✅ (no change) |
| `Vitamin C + Zinc` | `Vitamin C  Zinc` |
| `Price: $25.99` | `Price 25.99` |
| `50% off!` | `50 off` |
| `Smith & Jones Ltd.` | `Smith  Jones Ltd.` |
| `Test@123#ABC` | `Test123ABC` |
| `بانادول (500)` | `بانادول 500` |

### Customer Names

| Original | Sanitized |
|----------|-----------|
| `Ahmed Al-Rashid` | `Ahmed Al-Rashid` ✅ (no change) |
| `O'Connor` | `OConnor` |
| `Dr. Smith (MD)` | `Dr. Smith MD` |
| `Ahmed@email.com` | `Ahmedemail.com` |

## Implementation Details

### Location
File: `WasfatyInvoiceProcessor/Services/RemoteDatabaseService.cs`  
Method: `SanitizeText(string text)`

### Process Flow

1. **Fetch Invoice Data** from remote database
2. **For each invoice line**:
   - Get original description
   - Run through `SanitizeText()`
   - Log if changed (visible in Debug output)
3. **For customer name**:
   - Get original name
   - Run through `SanitizeText()`
   - Log if changed (visible in Debug output)
4. **Send cleaned data** to API

### Additional Processing

The sanitizer also:
- ✅ Trims leading/trailing whitespace
- ✅ Removes multiple consecutive spaces (replaces with single space)
- ✅ Preserves Arabic characters (full Unicode range 0x0600-0x06FF)

## Logging

When sanitization changes text, it's logged to Debug output:

```
Sanitized description: 'Panadol (500mg)' -> 'Panadol 500mg'
Sanitized customer name: 'Dr. Smith (MD)' -> 'Dr. Smith MD'
```

You can view these logs in Visual Studio's Output window (Debug mode) or in debug logs.

## Testing

### Manual Test

To verify sanitization is working:

1. **Check your data** - Look for product descriptions or customer names with special characters
2. **Process an invoice** - Run the application
3. **Check debug logs** - Look for "Sanitized description" or "Sanitized customer name" messages
4. **Verify API request** - Check the `APIRequest` table in your local database to see the actual JSON sent

### SQL Query to Find Data That Will Be Sanitized

```sql
-- Find descriptions with special characters
SELECT TOP 100
    b.Description,
    b.NativeName
FROM Items b
WHERE 
    b.Description LIKE '%(%' OR
    b.Description LIKE '%)%' OR
    b.Description LIKE '%+%' OR
    b.Description LIKE '%=%' OR
    b.Description LIKE '%&%' OR
    b.Description LIKE '%#%' OR
    b.Description LIKE '%@%' OR
    b.Description LIKE '%!%'
ORDER BY b.Id DESC;

-- Find customer names with special characters  
SELECT TOP 100 DISTINCT
    consumername
FROM invoices
WHERE 
    consumername LIKE '%(%' OR
    consumername LIKE '%)%' OR
    consumername LIKE '%+%' OR
    consumername LIKE '%@%'
ORDER BY Id DESC;
```

## Benefits

1. ✅ **Prevents API Errors** - Special characters can cause JSON parsing errors
2. ✅ **Improves Security** - Removes potentially problematic characters
3. ✅ **Ensures Consistency** - All data follows the same format
4. ✅ **Maintains Readability** - Keeps meaningful text while removing noise
5. ✅ **Preserves Arabic** - Full support for Arabic language text

## Configuration

The allowed characters are hard-coded in the `SanitizeText()` method. If you need to allow additional characters:

1. Open `RemoteDatabaseService.cs`
2. Find the `SanitizeText()` method
3. Add your character to the if condition:
   ```csharp
   c == '.' ||    // Period
   c == ',' ||    // Comma
   c == '-' ||    // Dash
   c == '/' ||    // Slash
   c == '_'       // Add underscore (example)
   ```

## Troubleshooting

### Issue: Text is being over-sanitized

**Symptom**: Too many characters are being removed  
**Solution**: Check the `SanitizeText()` method and add the character you want to keep

### Issue: Special character still appearing in API request

**Symptom**: A special character is in the API request despite sanitization  
**Solution**: The character might be in a field that's not sanitized (only `Description` and `CustomerName` are sanitized). Check which field contains it.

### Issue: Arabic text is being removed

**Symptom**: Arabic characters disappear  
**Solution**: Verify the Unicode range check `(c >= 0x0600 && c <= 0x06FF)` is present in the code

## Version History

- **v1.0** - Initial implementation with (, ), +, =, &, #, @, !, :, ;, % excluded
- **v2.0** (Current) - Enhanced sanitization with comprehensive special character removal
- Added debug logging for sanitization changes
- Added multiple space cleanup

---

**Note**: This sanitization happens **before** the data is sent to the API. The original data in your remote database remains unchanged.

